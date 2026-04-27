using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyNote;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double DebugMinimumWindowHeight = 552;
    private const double TopRightDockMargin = 20;

    #region Win32

    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string cls, string? win);
    [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string cls, string? win);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLongPtr32(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mod, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr hmon, ref MonitorInfo info);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;

        public static MonitorInfo Create() => new() { Size = 40 };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public IntPtr Hwnd;
        public IntPtr HwndInsertAfter;
        public int X;
        public int Y;
        public int Cx;
        public int Cy;
        public uint Flags;
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int GWLP_HWNDPARENT = -8;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_THICKFRAME = 0x00040000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_HIDEWINDOW = 0x0080;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOOWNERZORDER = 0x0200;
    private const uint SWP_NOSENDCHANGING = 0x0400;
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
    private const int HOTKEY_SHOW = 1;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const int WM_HOTKEY = 0x0312;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MINIMIZE = 0xF020;
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int WM_EXITSIZEMOVE = 0x0232;

    #endregion

    private static readonly string TodoStatePath = System.IO.Path.Combine(AppPaths.AppDataDirectory, "todos.json");

    private readonly ObservableCollection<TodoItem> _allItems = new();
    public ObservableCollection<TodoItem> VisibleItems { get; } = new();

    private readonly DispatcherTimer _deleteHoldTimer;
    private readonly DispatcherTimer _actionToggleTimer;
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
        Activated += (_, _) => LogWindowEvent("Activated");
        Deactivated += (_, _) => LogWindowEvent("Deactivated");
        LocationChanged += (_, _) => LogWindowEvent("LocationChanged");
        SizeChanged += (_, e) => LogWindowEvent("SizeChanged", $"WidthChanged={e.WidthChanged},HeightChanged={e.HeightChanged}");

        _deleteHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(520) };
        _deleteHoldTimer.Tick += DeleteHoldTimer_Tick;
        _actionToggleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime() + 20) };
        _actionToggleTimer.Tick += ActionToggleTimer_Tick;
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

    private void ApplyThemePalette()
    {
        if (IsNightTheme)
        {
            SetBrush("ChromeBackgroundBrush", "#242A22");
            SetBrush("ChromeBorderBrush", "#33465240");
            SetBrush("SurfaceBrush", "#30382D");
            SetBrush("CardSelectedSurfaceBrush", "#283022");
            SetBrush("PinnedHoverOverlayBrush", "#6A5D28");
            SetBrush("SurfaceStrongBrush", "#3A4435");
            SetBrush("EditSurfaceBrush", "#57664A");
            SetBrush("EditStatusBrush", "#D8C46C");
            SetBrush("SecondaryActionBrush", "#3A4435");
            SetBrush("SecondaryActionHoverBrush", "#46523F");
            SetBrush("SecondaryActionPressedBrush", "#20261F");
            SetBrush("SurfaceHoverBrush", "#46523F");
            SetBrush("SurfacePressedBrush", "#20261F");
            SetBrush("TextPrimaryBrush", "#EEF1E5");
            SetBrush("TextMutedBrush", "#BBC5AD");
            SetBrush("TextSubtleBrush", "#8D9980");
            SetBrush("NoteTextBrush", "#EEF1E5");
            SetBrush("NoteTextSubtleBrush", "#AEB89F");
            SetBrush("AccentBrush", "#D8C46C");
            SetBrush("AccentHoverBrush", "#E3D17D");
            SetBrush("AccentPressedBrush", "#BCA84F");
            SetBrush("HeaderIconHoverBrush", "#F0D76E");
            SetBrush("HeaderIconPressedBrush", "#FFE58A");
            SetBrush("PinnedIconBrush", "#F0D76E");
            SetBrush("PinnedSurfaceBrush", "#4A4325");
            SetBrush("PinnedBorderBrush", "#A9923E");
            SetBrush("DangerBrush", "#E28B7F");
            SetBrush("DoneCornerBrush", "#D7473F");
            SetBrush("OverlayBrush", "#F23F3328");
            SetBrush("ScrollTrackBrush", "#3346503E");
            SetBrush("ScrollThumbBrush", "#8895A083");
            return;
        }

        SetBrush("ChromeBackgroundBrush", "#ECE4D5");
        SetBrush("ChromeBorderBrush", "#40D8CEB8");
        SetBrush("SurfaceBrush", "#DED5C3");
        SetBrush("CardSelectedSurfaceBrush", "#D3CAB8");
        SetBrush("PinnedHoverOverlayBrush", "#E1CB7D");
        SetBrush("SurfaceStrongBrush", "#F4ECDC");
        SetBrush("EditSurfaceBrush", "#DEC985");
        SetBrush("EditStatusBrush", "#5F7D5C");
        SetBrush("SecondaryActionBrush", "#F4ECDC");
        SetBrush("SecondaryActionHoverBrush", "#E8DEC9");
        SetBrush("SecondaryActionPressedBrush", "#D7CCB7");
        SetBrush("SurfaceHoverBrush", "#E8DEC9");
        SetBrush("SurfacePressedBrush", "#D7CCB7");
        SetBrush("TextPrimaryBrush", "#2F291E");
        SetBrush("TextMutedBrush", "#756B5C");
        SetBrush("TextSubtleBrush", "#928674");
        SetBrush("NoteTextBrush", "#2F291E");
        SetBrush("NoteTextSubtleBrush", "#756B5C");
        SetBrush("AccentBrush", "#668564");
        SetBrush("AccentHoverBrush", "#73916E");
        SetBrush("AccentPressedBrush", "#557252");
        SetBrush("HeaderIconHoverBrush", "#557252");
        SetBrush("HeaderIconPressedBrush", "#3F5D3E");
        SetBrush("PinnedIconBrush", "#536E50");
        SetBrush("PinnedSurfaceBrush", "#E9D89D");
        SetBrush("PinnedBorderBrush", "#B59B3E");
        SetBrush("DangerBrush", "#B45C50");
        SetBrush("DoneCornerBrush", "#C9342E");
        SetBrush("OverlayBrush", "#F4E0CFC2");
        SetBrush("ScrollTrackBrush", "#2A2F291E");
        SetBrush("ScrollThumbBrush", "#7A756B5C");
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!WindowStateManager.RestorePosition(this))
        {
            ApplyDefaultWindowHeight();
            ApplyTopRightPlacement();
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

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        IsNightTheme = !IsNightTheme;
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

    // 通过将窗口 parent 到 SysListView32，而不是 WorkerW，
    // 避免在点击“显示桌面”时被 WorkerW 覆盖。
    private static IntPtr GetDesktopPtr()
    {
        var progman = FindWindow("Progman", "Program Manager");
        if (progman != IntPtr.Zero)
        {
            var shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                var desktop = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
                if (desktop != IntPtr.Zero) return desktop;
            }
        }

        IntPtr result = IntPtr.Zero;
        IntPtr candidate = IntPtr.Zero;
        do
        {
            candidate = FindWindowEx(IntPtr.Zero, candidate, "WorkerW", null);
            if (candidate != IntPtr.Zero)
            {
                var shellView = FindWindowEx(candidate, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    result = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
                    if (result != IntPtr.Zero) break;
                }
            }
        } while (candidate != IntPtr.Zero);

        return result;
    }

    private void EnterDesktopMode()
    {
        LogWindowEvent("EnterDesktopMode.Start");
        if (_hwnd == IntPtr.Zero || _hiddenByUser)
        {
            LogWindowEvent("EnterDesktopMode.Skip", $"HwndZero={_hwnd == IntPtr.Zero},HiddenByUser={_hiddenByUser}");
            return;
        }

        var desktop = GetDesktopPtr();
        if (desktop == IntPtr.Zero)
        {
            LogWindowEvent("EnterDesktopMode.NoDesktopHost");
            return;
        }

        SetWindowLong(_hwnd, GWL_EXSTYLE, GetWindowLong(_hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        SetWindowLongPtr64(_hwnd, GWLP_HWNDPARENT, desktop);
        SetWindowPos(_hwnd, HWND_BOTTOM, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
        var curStyle = GetWindowLong(_hwnd, GWL_STYLE);
        SetWindowLong(_hwnd, GWL_STYLE, curStyle | 0x00020000 | 0x00010000);

        var excluded = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excluded, sizeof(int));
        LogWindowEvent("EnterDesktopMode.Done", $"DesktopHost=0x{desktop.ToInt64():X}");
    }

    public void EnsureInteractiveMode()
    {
        LogWindowEvent("EnsureInteractiveMode.Start");
        if (_hwnd == IntPtr.Zero)
        {
            LogWindowEvent("EnsureInteractiveMode.Skip", "HwndZero=true");
            return;
        }
        SetWindowLongPtr64(_hwnd, GWLP_HWNDPARENT, IntPtr.Zero);
        SetWindowLong(_hwnd, GWL_EXSTYLE, _originalExStyle | WS_EX_TOOLWINDOW);
        // 直接 TOPMOST，防止脱离桌面嵌入瞬间被 WorkerW 遮住
        SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        Activate();
        Focus();
        LogWindowEvent("EnsureInteractiveMode.Done");
    }

    public void BeginDrag()
    {
        LogWindowEvent("BeginDrag.Start");
        try { DragMove(); } catch { }
        PersistWindowPlacement("BeginDrag");
        // 不立即重新嵌入，等用户下次与窗口交互时再嵌入，避免被 WorkerW 压住
        LogWindowEvent("BeginDrag.Done");
    }

    public void HideWindow()
    {
        LogWindowEvent("HideWindow.Before");
        _hiddenByUser = true;
        PersistWindowPlacement("HideWindow");
        Hide();
        LogWindowEvent("HideWindow.After");
    }

    public void ShowWindow()
    {
        LogWindowEvent("ShowWindow.Before");
        _hiddenByUser = false;
        Show();
        ReenterDesktopModeSoon();
        LogWindowEvent("ShowWindow.After");
    }

    public void DockToTopRight()
    {
        LogWindowEvent("DockToTopRight.Start");
        EnsureInteractiveMode();
        if (!ApplyTopRightPlacement())
            return;

        PersistWindowPlacement("DockToTopRight");
        ReenterDesktopModeSoon();
        LogWindowEvent("DockToTopRight.Done");
    }

    private bool ApplyTopRightPlacement()
    {
        var hmon = MonitorFromWindow(_hwnd, 2);
        var info = MonitorInfo.Create();
        if (!GetMonitorInfo(hmon, ref info))
        {
            LogWindowEvent("ApplyTopRightPlacement.NoMonitorInfo");
            return false;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY;
        var waRight = info.WorkArea.Right / scaleX;
        var waTop = info.WorkArea.Top / scaleY;
        Left = waRight - Width - TopRightDockMargin;
        Top = waTop + TopRightDockMargin;
        return true;
    }

    public void PersistWindowPlacement(string reason)
    {
        var saved = WindowStateManager.SavePosition(this);
        LogWindowEvent("PersistWindowPlacement", $"Reason={reason},Saved={saved}");
    }

    private void ReenterDesktopModeSoon()
    {
        LogWindowEvent("ReenterDesktopModeSoon.Schedule");
        Dispatcher.BeginInvoke(EnterDesktopMode, DispatcherPriority.ApplicationIdle);
    }

    private void LoadTodos()
    {
        LogWindowEvent("LoadTodos.Start");
        try
        {
            if (!System.IO.File.Exists(TodoStatePath))
            {
                RefreshVisibleItems();
                LogWindowEvent("LoadTodos.MissingFile", TodoStatePath);
                return;
            }

            var json = System.IO.File.ReadAllText(TodoStatePath);
            var items = JsonSerializer.Deserialize<List<TodoItem>>(json);
            _allItems.Clear();
            foreach (var item in items ?? [])
            {
                _allItems.Add(item);
            }
        }
        catch
        {
            _allItems.Clear();
            LogWindowEvent("LoadTodos.Error");
        }

        RefreshVisibleItems();
        LogWindowEvent("LoadTodos.Done", $"ItemCount={_allItems.Count}");
    }

    private void SaveTodos()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(TodoStatePath)!;
            System.IO.Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_allItems);
            System.IO.File.WriteAllText(TodoStatePath, json);
            LogWindowEvent("SaveTodos.Done", $"ItemCount={_allItems.Count}");
        }
        catch
        {
            LogWindowEvent("SaveTodos.Error");
        }
    }

    private void ClearPendingDelete(bool currentViewOnly = true)
    {
        CancelPendingActionToggle();

        var items = _allItems.Where(item => item.PendingDelete);
        if (currentViewOnly)
        {
            items = items.Where(CanToggleDoneFromCurrentView);
        }

        foreach (var item in items)
        {
            item.PendingDelete = false;
        }

        if (_pendingDeleteItem is null || !currentViewOnly || CanToggleDoneFromCurrentView(_pendingDeleteItem))
        {
            _pendingDeleteItem = null;
        }

        _deleteHoldTimer.Stop();
    }

    private void ClearTodoActions(TodoItem? except = null, bool currentViewOnly = true)
    {
        var items = _allItems.Where(item => item != except && (item.ActionOpen || item.Completing));
        if (currentViewOnly)
        {
            items = items.Where(CanToggleDoneFromCurrentView);
        }

        foreach (var item in items)
        {
            item.ActionOpen = false;
            item.Completing = false;
            item.SuppressActionOpenAnimation = false;
        }
    }

    private void ClearTodoEditing(TodoItem? except = null)
    {
        foreach (var item in _allItems.Where(item => item != except && item.IsEditing))
        {
            item.EditingText = item.Text;
            item.IsEditing = false;
            if (item == _editingTodoItem)
            {
                _editingTodoItem = null;
            }
        }

        NotifyDraftPanelStateChanged();
    }

    private void CancelPendingActionToggle()
    {
        _actionToggleTimer.Stop();
        _pendingActionToggleItem = null;
    }

    private void QueueActionToggle(TodoItem item)
    {
        CancelPendingActionToggle();
        _pendingActionToggleItem = item;
        _actionToggleTimer.Start();
    }

    private void ActionToggleTimer_Tick(object? sender, EventArgs e)
    {
        _actionToggleTimer.Stop();

        if (_pendingActionToggleItem is not { } item || item.PendingDelete || item.IsEditing)
        {
            _pendingActionToggleItem = null;
            return;
        }

        if (CanToggleDoneFromCurrentView(item))
        {
            if (item.ActionOpen)
            {
                item.ActionOpen = false;
                item.SuppressActionOpenAnimation = false;
            }
            else
            {
                item.SuppressActionOpenAnimation = false;
                ClearTodoActions(item);
                item.ActionOpen = true;
            }

            LogWindowEvent("TodoItem_Click.ActionToggle", $"ItemId={item.Id},ActionOpen={item.ActionOpen}");
        }
        else
        {
            ClearTodoActions();
        }

        _pendingActionToggleItem = null;
    }

    private void NotifyDraftPanelStateChanged()
    {
        OnPropertyChanged(nameof(IsDraftEditMode));
        OnPropertyChanged(nameof(DraftPrimaryActionText));
        OnPropertyChanged(nameof(DraftPanelStatusText));
        OnPropertyChanged(nameof(HasDraftText));
    }

    private bool CanToggleDoneFromCurrentView(TodoItem item)
        => IsPendingView ? item.CompletedAt is null : item.CompletedAt is not null;

    private void RefreshVisibleItems()
    {
        VisibleItems.Clear();

        IEnumerable<TodoItem> items = _viewMode switch
        {
            ViewMode.Pending => _allItems
                .Where(item => item.CompletedAt is null)
                .OrderByDescending(item => item.Pinned)
                .ThenByDescending(item => item.CreatedAt),
            _ => _allItems
                .Where(item => item.CompletedAt is not null)
                .OrderByDescending(item => item.CompletedAt)
        };

        foreach (var item in items)
        {
            VisibleItems.Add(item);
        }

        OnPropertyChanged(nameof(HasVisibleItems));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(IsPendingView));
        OnPropertyChanged(nameof(IsDraftPanelActiveInCurrentView));
        OnPropertyChanged(nameof(IsSettingsPanelActiveInCurrentView));
        OnPropertyChanged(nameof(PendingTabTag));
        OnPropertyChanged(nameof(CompletedTabTag));
        OnPropertyChanged(nameof(DeleteOverlayText));
        LogWindowEvent("RefreshVisibleItems", $"All={_allItems.Count},Visible={VisibleItems.Count},ViewMode={_viewMode}");
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        BeginDrag();
        e.Handled = true;
        LogWindowEvent("Header_MouseLeftButtonDown.Handled");
    }

    private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || e.Handled)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && IsInteractiveChromeSource(source))
        {
            return;
        }

        BeginDrag();
        e.Handled = true;
        LogWindowEvent("Chrome_MouseLeftButtonDown.Handled");
    }

    private void TopResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isTopResizeDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreen = PointToScreen(e.GetPosition(this));
        var deltaY = currentScreen.Y - _resizeDragStartScreen.Y;
        var nextHeight = Math.Clamp(_resizeDragStartHeight - deltaY, MinHeight, MaxHeight);
        var bottom = _resizeDragStartTop + _resizeDragStartHeight;

        Height = nextHeight;
        Top = bottom - nextHeight;
    }

    private void TopResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isTopResizeDragging)
        {
            return;
        }

        EndResizeGripDrag("TopResizeGrip_MouseLeftButtonUp");
        e.Handled = true;
    }

    private void TopResizeGrip_LostMouseCapture(object sender, MouseEventArgs e)
        => EndResizeGripDrag("TopResizeGrip_LostMouseCapture");

    private static bool IsInteractiveChromeSource(DependencyObject source)
        => FindParent<Button>(source) is not null
            || FindParent<TextBox>(source) is not null
            || FindParent<Slider>(source) is not null
            || FindParent<ScrollBar>(source) is not null
            || FindParent<ListBoxItem>(source) is not null;

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = !IsSettingsOpen;
        if (IsSettingsOpen)
        {
            CloseDraftPanel();
        }
        LogWindowEvent("SettingsButton_Click", $"IsSettingsOpen={IsSettingsOpen}");
    }

    private void AddTodoToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        ClearTodoActions();

        if (IsDraftEditMode)
        {
            CancelTodoEdit(_editingTodoItem);
        }

        IsDraftOpen = !IsDraftOpen;
        if (IsDraftOpen)
        {
            IsSettingsOpen = false;
            DraftText = _addDraftBuffer;
            FocusDraftInput(selectAll: string.IsNullOrWhiteSpace(DraftText) is false);
        }
        LogWindowEvent("AddTodoToggleButton_Click", $"IsDraftOpen={IsDraftOpen}");
    }

    private void DraftInputTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ClearPendingDelete();
        ClearTodoActions();
        ClearTodoEditing(_editingTodoItem);
        LogWindowEvent("DraftInputTextBox_GotKeyboardFocus");
    }

    private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = false;
        LogWindowEvent("SettingsCloseButton_Click");
    }

    private void PendingTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(ViewMode.Pending);
        LogWindowEvent("PendingTab_Click");
    }

    private void CompletedTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(ViewMode.Completed);
        LogWindowEvent("CompletedTab_Click");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsDraftEditMode)
        {
            if (_editingTodoItem is null || !CommitTodoEdit(_editingTodoItem))
            {
                LogWindowEvent("AddButton_Click.SkipEditEmpty");
                return;
            }

            LogWindowEvent("AddButton_Click.EditDone");
            return;
        }

        if (!TryAddTodo(DraftText, out _))
        {
            LogWindowEvent("AddButton_Click.SkipEmpty");
            return;
        }

        _addDraftBuffer = string.Empty;
        DraftText = string.Empty;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("AddButton_Click.Done");
    }

    private async void ToggleDoneButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ClearPendingDelete();
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("ToggleDoneButton_Click.InvalidTag");
            return;
        }

        if (CanToggleDoneFromCurrentView(item))
        {
            item.ActionOpen = true;
            item.Completing = true;
            LogWindowEvent("ToggleDoneButton_Click.Animate", $"ItemId={item.Id}");
            await Task.Delay(230);
        }

        ToggleDone(item);
        LogWindowEvent("ToggleDoneButton_Click.Done", $"ItemId={item.Id},CompletedAt={(item.CompletedAt is null ? "null" : item.CompletedAt.Value.ToString("O"))}");
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("PinButton_Click.InvalidTag");
            return;
        }

        TogglePinned(item);
        LogWindowEvent("PinButton_Click.Done", $"ItemId={item.Id},Pinned={item.Pinned}");
    }

    private void TodoItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CancelPendingActionToggle();

        if (IsDraftPanelActiveInCurrentView)
        {
            ClearPendingDelete();
            ClearTodoActions();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            LogWindowEvent("TodoItem_MouseLeftButtonDown.SkipDraftOpen");
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject textSource && FindParent<TextBox>(textSource) is not null)
        {
            return;
        }

        if (sender is not Border { Tag: TodoItem item })
        {
            return;
        }

        if (_allItems.Any(todo => todo.PendingDelete))
        {
            ClearPendingDelete();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            e.Handled = true;
            LogWindowEvent("TodoItem_MouseLeftButtonDown.ClearPendingDeleteOnly", $"ItemId={item.Id}");
            return;
        }

        if (e.ClickCount >= 2)
        {
            _deleteHoldTimer.Stop();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            BeginTodoEdit(item);
            e.Handled = true;
            LogWindowEvent("TodoItem_MouseLeftButtonDown.BeginEdit", $"ItemId={item.Id}");
            return;
        }

        if (item.IsEditing)
        {
            ClearPendingDelete();
            _pressedTodoItem = item;
            _pendingDeleteItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            return;
        }

        if (item.ActionOpen || item.Completing)
        {
            ClearPendingDelete();
            _pressedTodoItem = item;
            _pendingDeleteItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            LogWindowEvent("TodoItem_MouseLeftButtonDown.SkipDeleteHold", $"ItemId={item.Id},ActionOpen={item.ActionOpen},Completing={item.Completing}");
            return;
        }

        ClearPendingDelete();
        _pendingDeleteItem = item;
        _pressedTodoItem = item;
        _deleteHoldTriggered = false;
        _deleteHoldTimer.Stop();
        _deleteHoldTimer.Start();
    }

    private void TodoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _deleteHoldTimer.Stop();
        if (IsDraftPanelActiveInCurrentView)
        {
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            ClearTodoActions();
            LogWindowEvent("TodoItem_MouseLeftButtonUp.SkipDraftOpen");
            return;
        }

        if (sender is Border { Tag: TodoItem item } && item == _pressedTodoItem && !_deleteHoldTriggered && !item.PendingDelete)
        {
            if (!item.IsEditing && CanToggleDoneFromCurrentView(item))
            {
                QueueActionToggle(item);
            }
            else
            {
                ClearTodoActions();
            }
        }

        _pendingDeleteItem = null;
        _pressedTodoItem = null;
        _deleteHoldTriggered = false;
    }

    private void TodoItem_MouseLeave(object sender, MouseEventArgs e)
    {
        _deleteHoldTimer.Stop();
        CancelPendingActionToggle();
        _pendingDeleteItem = null;
        _pressedTodoItem = null;
        _deleteHoldTriggered = false;
    }

    private void DeleteHoldTimer_Tick(object? sender, EventArgs e)
    {
        _deleteHoldTimer.Stop();
        if (_pendingDeleteItem is null)
        {
            LogWindowEvent("DeleteHoldTimer_Tick.Skip");
            return;
        }

        if (_pendingDeleteItem.ActionOpen || _pendingDeleteItem.Completing)
        {
            LogWindowEvent("DeleteHoldTimer_Tick.SkipActionOpen", $"ItemId={_pendingDeleteItem.Id},ActionOpen={_pendingDeleteItem.ActionOpen},Completing={_pendingDeleteItem.Completing}");
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            return;
        }

        if (_pendingDeleteItem.IsEditing)
        {
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            return;
        }

        foreach (var item in _allItems.Where(item => item.PendingDelete && CanToggleDoneFromCurrentView(item)))
        {
            item.PendingDelete = false;
        }

        _deleteHoldTriggered = true;
        _pendingDeleteItem.ActionOpen = false;
        _pendingDeleteItem.Completing = false;
        _pendingDeleteItem.PendingDelete = true;
        LogWindowEvent("DeleteHoldTimer_Tick.Done", $"ItemId={_pendingDeleteItem.Id}");
    }

    private void DeleteConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("DeleteConfirmButton_Click.InvalidTag");
            return;
        }

        DeleteTodo(item);
        LogWindowEvent("DeleteConfirmButton_Click.Done", $"ItemId={item.Id}");
    }

    private void CancelDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        LogWindowEvent("CancelDeleteButton_Click");
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        HideWindow();
        LogWindowEvent("HideButton_Click");
    }

    private void DraftCancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsDraftEditMode)
        {
            CancelTodoEdit(_editingTodoItem);
            return;
        }

        IsDraftOpen = false;
        LogWindowEvent("DraftCancelButton_Click.CloseDraft");
    }

    private void DraftTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (IsDraftEditMode)
            {
                CancelTodoEdit(_editingTodoItem);
            }
            else
            {
                IsDraftOpen = false;
                LogWindowEvent("DraftTextBox_PreviewKeyDown.CloseDraft");
            }
            return;
        }

        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        e.Handled = true;
        AddButton_Click(sender, new RoutedEventArgs());
        LogWindowEvent("DraftTextBox_PreviewKeyDown.Submit");
    }

    private void SwitchView(ViewMode viewMode)
    {
        CancelPendingActionToggle();
        foreach (var item in _allItems.Where(item => item.ActionOpen))
        {
            item.SuppressActionOpenAnimation = true;
        }

        _viewMode = viewMode;
        RefreshVisibleItems();
    }

    private void BeginTodoEdit(TodoItem item)
    {
        ClearPendingDelete();
        CancelPendingActionToggle();
        ClearTodoActions(item);
        ClearTodoEditing(item);
        _addDraftBuffer = DraftText;

        if (_editingTodoItem is not null && _editingTodoItem != item)
        {
            _editingTodoItem.EditingText = _editingTodoItem.Text;
            _editingTodoItem.IsEditing = false;
        }

        _editingTodoItem = item;
        item.ActionOpen = false;
        item.Completing = false;
        item.SuppressActionOpenAnimation = false;
        item.EditingText = item.Text;
        item.IsEditing = true;
        DraftText = item.Text;
        IsDraftOpen = true;
        IsSettingsOpen = false;
        NotifyDraftPanelStateChanged();
        FocusDraftInput(selectAll: true);
    }

    private bool CommitTodoEdit(TodoItem item)
    {
        var normalized = DraftText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            LogWindowEvent("CommitTodoEdit.SkipEmpty", $"ItemId={item.Id}");
            return false;
        }

        ClearPendingDelete();
        CancelPendingActionToggle();
        item.Text = normalized;
        item.EditingText = normalized;
        item.IsEditing = false;
        _editingTodoItem = null;
        SaveTodos();
        RefreshVisibleItems();
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("CommitTodoEdit.Done", $"ItemId={item.Id}");
        return true;
    }

    private void CancelTodoEdit(TodoItem? item)
    {
        CancelPendingActionToggle();
        if (item is null)
        {
            CloseDraftPanel();
            return;
        }

        item.EditingText = item.Text;
        item.IsEditing = false;
        _editingTodoItem = null;
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("CancelTodoEdit.Done", $"ItemId={item.Id}");
    }

    private void CloseDraftPanel()
    {
        if (_editingTodoItem is not null)
        {
            _editingTodoItem.EditingText = _editingTodoItem.Text;
            _editingTodoItem.IsEditing = false;
            _editingTodoItem = null;
        }

        IsDraftOpen = false;
        DraftText = _addDraftBuffer;
        NotifyDraftPanelStateChanged();
    }

    private void FocusDraftInput(bool selectAll)
    {
        Dispatcher.BeginInvoke(() =>
        {
            DraftInputTextBox.Focus();
            Keyboard.Focus(DraftInputTextBox);
            if (selectAll)
            {
                DraftInputTextBox.SelectAll();
            }
            else
            {
                DraftInputTextBox.CaretIndex = DraftInputTextBox.Text.Length;
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    private bool TryAddTodo(string text, out TodoItem? item)
    {
        ClearPendingDelete();
        ClearTodoActions();
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            item = null;
            return false;
        }

        item = new TodoItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = normalized,
            CreatedAt = DateTime.Now,
            IsNew = true
        };

        _allItems.Insert(0, item);
        DraftText = string.Empty;
        SaveTodos();
        RefreshVisibleItems();
        ClearNewItemFlagAfterEntrance(item);
        return true;
    }

    private async void ClearNewItemFlagAfterEntrance(TodoItem item)
    {
        await Task.Delay(420);
        if (_allItems.Contains(item))
        {
            item.IsNew = false;
        }
    }

    private void ToggleDone(TodoItem item)
    {
        ClearPendingDelete();
        if (item.CompletedAt is null)
        {
            item.CompletedAt = DateTime.Now;
            item.Pinned = false;
        }
        else
        {
            item.CompletedAt = null;
        }

        item.ActionOpen = false;
        item.Completing = false;
        item.SuppressActionOpenAnimation = false;
        SaveTodos();
        RefreshVisibleItems();
    }

    private void TogglePinned(TodoItem item)
    {
        ClearPendingDelete();
        ClearTodoActions(item);
        item.Pinned = !item.Pinned;
        SaveTodos();
        RefreshVisibleItems();
    }

    private void DeleteTodo(TodoItem item)
    {
        _allItems.Remove(item);
        SaveTodos();
        RefreshVisibleItems();
        ClearPendingDelete();
    }

    internal TodoItem AddTodoForAutomation(string text)
    {
        if (!TryAddTodo(text, out var item) || item is null)
            throw new InvalidOperationException("Unable to add todo item.");

        return item;
    }

    internal void SetViewModeForAutomation(ViewMode viewMode) => SwitchView(viewMode);

    internal void ToggleDoneForAutomation(TodoItem item) => ToggleDone(item);

    internal void TogglePinnedForAutomation(TodoItem item) => TogglePinned(item);

    internal void BeginEditForAutomation(TodoItem item) => BeginTodoEdit(item);

    internal bool CommitEditForAutomation(TodoItem item, string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        item.Text = normalized;
        item.EditingText = normalized;
        item.IsEditing = false;
        _editingTodoItem = null;
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        SaveTodos();
        RefreshVisibleItems();
        NotifyDraftPanelStateChanged();
        return true;
    }

    internal void CancelEditForAutomation(TodoItem item) => CancelTodoEdit(item);

    internal void MarkPendingDeleteForAutomation(TodoItem item)
    {
        ClearPendingDelete();
        _pendingDeleteItem = item;
        DeleteHoldTimer_Tick(this, EventArgs.Empty);
    }

    internal void CancelPendingDeleteForAutomation() => ClearPendingDelete();

    internal void DeleteTodoForAutomation(TodoItem item) => DeleteTodo(item);

    internal IReadOnlyList<TodoItem> SnapshotItemsForAutomation() => _allItems.ToList();

    internal TodoItem? FindTodoForAutomation(string id) => _allItems.FirstOrDefault(item => item.Id == id);

    internal bool TriggerHotkeyForAutomation()
        => _hwnd != IntPtr.Zero && PostMessage(_hwnd, WM_HOTKEY, new IntPtr(HOTKEY_SHOW), IntPtr.Zero);

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _pendingDesktopReenterAfterResize = true;
        EnsureInteractiveMode();
        _isResizeDragging = true;
        _isTopResizeDragging = false;
        _resizeDragStartScreen = PointToScreen(e.GetPosition(this));
        _resizeDragStartHeight = Height;
        _resizeDragStartTop = Top;
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
        LogWindowEvent("ResizeGrip_MouseLeftButtonDown.Handled");
    }

    private void TopResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        BeginTopResizeGripDrag(sender, e);
        e.Handled = true;
    }

    private void BeginTopResizeGripDrag(object sender, MouseButtonEventArgs e)
    {
        _pendingDesktopReenterAfterResize = true;
        EnsureInteractiveMode();
        _isResizeDragging = true;
        _isTopResizeDragging = true;
        _resizeDragStartScreen = PointToScreen(e.GetPosition(this));
        _resizeDragStartHeight = Height;
        _resizeDragStartTop = Top;
        Mouse.Capture(sender as IInputElement);
        LogWindowEvent("TopResizeGrip_MouseLeftButtonDown.Handled");
    }

    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isResizeDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreen = PointToScreen(e.GetPosition(this));
        var deltaY = currentScreen.Y - _resizeDragStartScreen.Y;
        Height = Math.Clamp(_resizeDragStartHeight + deltaY, MinHeight, MaxHeight);
    }

    private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndResizeGripDrag("ResizeGrip_MouseLeftButtonUp");
        e.Handled = true;
    }

    private void ResizeGrip_LostMouseCapture(object sender, MouseEventArgs e)
        => EndResizeGripDrag("ResizeGrip_LostMouseCapture");

    private void EndResizeGripDrag(string reason)
    {
        if (!_isResizeDragging)
        {
            return;
        }

        _isResizeDragging = false;
        _isTopResizeDragging = false;
        if (Mouse.Captured is not null)
        {
            Mouse.Capture(null);
        }

        PersistWindowPlacement(reason);
        if (_pendingDesktopReenterAfterResize)
        {
            ReenterDesktopModeSoon();
            _pendingDesktopReenterAfterResize = false;
        }

        LogWindowEvent("EndResizeGripDrag", $"Reason={reason}");
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

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLongPtr32(hwnd, index));

    private void LogWindowEvent(string eventName, string? extra = null)
    {
        var hwnd = _hwnd;
        var style = hwnd == IntPtr.Zero ? 0 : GetWindowLong(hwnd, GWL_STYLE);
        var exStyle = hwnd == IntPtr.Zero ? 0 : GetWindowLong(hwnd, GWL_EXSTYLE);
        var parent = hwnd == IntPtr.Zero ? IntPtr.Zero : GetWindowLongPtr(hwnd, GWLP_HWNDPARENT);
        var summary = $"{eventName} | hiddenByUser={_hiddenByUser},visible={IsVisible},active={IsActive},windowState={WindowState},left={Left:0.##},top={Top:0.##},width={Width:0.##},height={Height:0.##},hwnd=0x{hwnd.ToInt64():X},parent=0x{parent.ToInt64():X},style=0x{style:X8},exStyle=0x{exStyle:X8}";
        if (!string.IsNullOrWhiteSpace(extra))
            summary += $" | {extra}";

        WindowEventLogger.Write("MainWindow", summary);
    }

    private static T? FindParent<T>(DependencyObject source) where T : DependencyObject
    {
        var current = source;
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum ViewMode
{
    Pending,
    Completed
}
