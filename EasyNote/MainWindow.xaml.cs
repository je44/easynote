using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace EasyNote;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double DebugMinimumWindowHeight = 552;
    private const double TopRightDockMargin = 20;
    private static readonly TimeSpan ExternalWindowDesktopReentryDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan TrayRestoreDesktopReentryDelay = TimeSpan.FromMilliseconds(80);

    private static readonly string TodoStatePath = LocalUserDataStore.TodoStatePath;

    private readonly ObservableCollection<TodoItem> _allItems = new();
    public ObservableCollection<TodoItem> VisibleItems { get; } = new();

    private readonly DispatcherTimer _deleteHoldTimer;
    private readonly DispatcherTimer _actionToggleTimer;
    private readonly DispatcherTimer _desktopReentryTimer;
    private TodoItem? _pendingDeleteItem;
    private TodoItem? _pressedTodoItem;
    private TodoItem? _pendingActionToggleItem;
    private TodoItem? _editingTodoItem;
    private bool _deleteHoldTriggered;
    private bool _isResizeDragging;
    private bool _isTopResizeDragging;
    private Point _resizeDragStartScreen;
    private double _resizeDragStartHeight;
    private double _resizeDragStartTop;

    private IntPtr _hwnd;
    private int _originalExStyle;
    private bool _hiddenByUser;
    private bool _pendingDesktopReenterAfterResize;
    private bool _reenterDesktopOnNextDeactivation;
    private bool _desktopReentryRequiresExternalForeground;
    private bool _isSettingsOpen;
    private bool _isNightTheme;
    private bool _isDraftOpen;
    private double _opacityPercent = 75;
    private string _draftText = string.Empty;
    private string _addDraftBuffer = string.Empty;
    private ViewMode _viewMode = ViewMode.Pending;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        MinHeight = DebugMinimumWindowHeight;
#endif
        DataContext = this;
        Loaded += OnLoaded;
        StateChanged += (_, _) =>
        {
            if (WindowState == System.Windows.WindowState.Minimized && !_hiddenByUser)
                WindowState = System.Windows.WindowState.Normal;
        };
        IsVisibleChanged += (_, e) =>
        {
            if (!(bool)e.NewValue && !_hiddenByUser)
                Dispatcher.BeginInvoke(Show, DispatcherPriority.ApplicationIdle);

            LogWindowEvent("IsVisibleChanged", $"NewValue={e.NewValue}");
        };
        Activated += (_, _) =>
        {
            LogWindowEvent("Activated");
            if (_desktopReentryRequiresExternalForeground)
            {
                _desktopReentryTimer?.Stop();
            }
        };
        Deactivated += (_, _) =>
        {
            LogWindowEvent("Deactivated");
            if (_reenterDesktopOnNextDeactivation && !_hiddenByUser && IsVisible)
            {
                ReenterDesktopModeSoon(ExternalWindowDesktopReentryDelay, requireExternalForeground: true);
            }
        };
        LocationChanged += (_, _) => LogWindowEvent("LocationChanged");
        SizeChanged += (_, e) => LogWindowEvent("SizeChanged", $"WidthChanged={e.WidthChanged},HeightChanged={e.HeightChanged}");

        _deleteHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(520) };
        _deleteHoldTimer.Tick += DeleteHoldTimer_Tick;
        _actionToggleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime() + 20) };
        _actionToggleTimer.Tick += ActionToggleTimer_Tick;
        _desktopReentryTimer = new DispatcherTimer { Interval = ExternalWindowDesktopReentryDelay };
        _desktopReentryTimer.Tick += (_, _) =>
        {
            _desktopReentryTimer.Stop();
            TryCompleteDeferredDesktopReentry();
        };
        ApplyThemePalette();
        LogWindowEvent("Ctor");
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (_isSettingsOpen == value) return;
            _isSettingsOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSettingsPanelActiveInCurrentView));
        }
    }

    public bool IsDraftOpen
    {
        get => _isDraftOpen;
        set
        {
            if (_isDraftOpen == value) return;
            _isDraftOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDraftPanelActiveInCurrentView));
        }
    }

    public double OpacityPercent
    {
        get => _opacityPercent;
        set
        {
            var next = Math.Clamp(value, 35, 100);
            if (Math.Abs(_opacityPercent - next) < 0.1) return;
            _opacityPercent = next;
            Opacity = _opacityPercent / 100d;
            OnPropertyChanged();
            if (IsLoaded)
                WindowStateManager.SavePosition(this);
        }
    }

    public bool IsNightTheme
    {
        get => _isNightTheme;
        set
        {
            if (_isNightTheme == value) return;
            _isNightTheme = value;
            ApplyThemePalette();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ThemeToggleText));
            if (IsLoaded)
                WindowStateManager.SavePosition(this);
        }
    }

    public string ThemeToggleText => IsNightTheme ? "切换到日间护眼" : "切换到夜间护眼";

    public string DraftText
    {
        get => _draftText;
        set
        {
            if (_draftText == value) return;
            _draftText = value;
            if (!IsDraftEditMode)
            {
                _addDraftBuffer = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDraftText));
        }
    }

    public bool IsPendingView => _viewMode == ViewMode.Pending;
    public bool IsDraftPanelActiveInCurrentView => IsPendingView && IsDraftOpen;
    public bool IsSettingsPanelActiveInCurrentView => IsPendingView && IsSettingsOpen;
    public bool HasDraftText => !string.IsNullOrWhiteSpace(DraftText);
    public bool IsDraftEditMode => _editingTodoItem is not null;
    public string DraftPrimaryActionText => IsDraftEditMode ? "提交" : "记下";
    public string DraftPanelStatusText => IsDraftEditMode ? "编辑中" : "新增待办";
    public string PendingTabTag => IsPendingView ? "Active" : string.Empty;
    public string CompletedTabTag => !IsPendingView ? "Active" : string.Empty;
    public string DeleteOverlayText => IsPendingView ? "删除这条待办？" : "删除这条已办？";
    public bool HasVisibleItems => VisibleItems.Count > 0;
    public string EmptyStateText => IsPendingView ? "还没有待办" : "还没有已办记录";

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!WindowStateManager.RestorePosition(this))
        {
            ApplyDefaultWindowHeight();
            ApplyTopRightPlacement();
            PersistWindowPlacement("OnLoaded.DefaultPlacement");
        }

        LoadTodos();
        ShowInTaskbar = true;
        ShowInTaskbar = false;
        LogWindowEvent("OnLoaded.BeforeEnterDesktopMode");
        EnterDesktopMode();
        LogWindowEvent("OnLoaded.AfterEnterDesktopMode");
    }

    private void ApplyDefaultWindowHeight()
    {
        var workArea = SystemParameters.WorkArea;
#if DEBUG
        var targetHeight = Math.Clamp(820d, MinHeight, MaxHeight);
#else
        var targetHeight = Math.Clamp(workArea.Height * 0.85, MinHeight, MaxHeight);
#endif
        var maxTop = Math.Max(workArea.Top, workArea.Bottom - targetHeight);

        Height = targetHeight;
        Top = Math.Clamp(Top, workArea.Top, maxTop);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        _originalExStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        // 加 WS_THICKFRAME 使底部拖拽 resize 生效
        // 加 WS_MINIMIZEBOX|WS_MAXIMIZEBOX：Windows "显示桌面"只最小化有这两个标志的窗口，
        // 缺失时 Windows 会直接 Hide 该窗口，导致无法通过普通方式恢复
        var style = GetWindowLong(_hwnd, GWL_STYLE);
        SetWindowLong(_hwnd, GWL_STYLE, style | WS_THICKFRAME | 0x00020000 | 0x00010000);
        HwndSource.FromHwnd(_hwnd)?.AddHook(WndProc);
        HideWinTab();
        var registered = RegisterHotKey(_hwnd, HOTKEY_SHOW, MOD_CTRL | MOD_ALT, (uint)KeyInterop.VirtualKeyFromKey(Key.N));
        LogWindowEvent("OnSourceInitialized", $"HotKeyRegistered={registered}");
    }

    private void HideWinTab()
    {
        var ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TOOLWINDOW;
        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
        LogWindowEvent("HideWinTab");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_SHOW)
        {
            LogWindowEvent("WndProc.WM_HOTKEY");
            if (IsVisible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }

            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
        {
            LogWindowEvent("WndProc.WM_SYSCOMMAND.Minimize");
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_WINDOWPOSCHANGING && !_hiddenByUser)
        {
            LogWindowEvent("WndProc.WM_WINDOWPOSCHANGING");
            unsafe
            {
                var pos = (WindowPos*)lParam;
                if (pos != null)
                    pos->Flags &= ~SWP_HIDEWINDOW;
            }
        }

        if (msg == WM_EXITSIZEMOVE)
        {
            if (!_hiddenByUser)
            {
                PersistWindowPlacement("WM_EXITSIZEMOVE");
                if (_pendingDesktopReenterAfterResize)
                    ReenterDesktopModeSoon();
            }

            _pendingDesktopReenterAfterResize = false;
            LogWindowEvent("WndProc.WM_EXITSIZEMOVE");
        }

        return IntPtr.Zero;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        PersistWindowPlacement("OnClosing");
        LogWindowEvent("OnClosing");
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, HOTKEY_SHOW);
        LogWindowEvent("OnClosed");
        base.OnClosed(e);
    }
}

public enum ViewMode
{
    Pending,
    Completed
}
