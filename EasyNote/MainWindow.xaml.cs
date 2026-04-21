using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyNote;

public partial class MainWindow : Window, INotifyPropertyChanged
{
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
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lp);
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

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lp);

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

    private static readonly string TodoStatePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "easy-note",
        "todos.json");

    private readonly ObservableCollection<TodoItem> _allItems = new();
    public ObservableCollection<TodoItem> VisibleItems { get; } = new();

    private readonly DispatcherTimer _deleteHoldTimer;
    private TodoItem? _pendingDeleteItem;

    private IntPtr _hwnd;
    private int _originalExStyle;
    private bool _hiddenByUser;
    private bool _isSettingsOpen;
    private double _opacityPercent = 75;
    private string _draftText = string.Empty;
    private ViewMode _viewMode = ViewMode.Pending;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
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
        }
    }

    public string DraftText
    {
        get => _draftText;
        set
        {
            if (_draftText == value) return;
            _draftText = value;
            OnPropertyChanged();
        }
    }

    public bool IsPendingView => _viewMode == ViewMode.Pending;
    public string PendingTabTag => IsPendingView ? "Active" : string.Empty;
    public string CompletedTabTag => !IsPendingView ? "Active" : string.Empty;
    public string DeleteOverlayText => IsPendingView ? "删除这条待办？" : "删除这条已办？";
    public bool HasVisibleItems => VisibleItems.Count > 0;
    public string EmptyStateText => IsPendingView ? "还没有待办" : "还没有已办记录";

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        WindowStateManager.RestorePosition(this);
        LoadTodos();
        ShowInTaskbar = true;
        ShowInTaskbar = false;
        LogWindowEvent("OnLoaded.BeforeEnterDesktopMode");
        EnterDesktopMode();
        LogWindowEvent("OnLoaded.AfterEnterDesktopMode");
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
        WindowStateManager.SavePosition(Left, Top, Width, Height);
        // 不立即重新嵌入，等用户下次与窗口交互时再嵌入，避免被 WorkerW 压住
        LogWindowEvent("BeginDrag.Done");
    }

    public void HideWindow()
    {
        LogWindowEvent("HideWindow.Before");
        _hiddenByUser = true;
        WindowStateManager.SavePosition(Left, Top, Width, Height);
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
        var hmon = MonitorFromWindow(_hwnd, 2);
        var info = MonitorInfo.Create();
        if (!GetMonitorInfo(hmon, ref info))
        {
            LogWindowEvent("DockToTopRight.NoMonitorInfo");
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var scaleX = dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY;
        var waRight = info.WorkArea.Right / scaleX;
        var waTop = info.WorkArea.Top / scaleY;
        Left = waRight - Width - 20;
        Top = waTop + 20;
        WindowStateManager.SavePosition(Left, Top, Width, Height);
        ReenterDesktopModeSoon();
        LogWindowEvent("DockToTopRight.Done");
    }

    public void ResizeHeight(double height)
    {
        Height = Math.Clamp(height, MinHeight, 920);
        WindowStateManager.SavePosition(Left, Top, Width, Height);
    }

    public void SetWindowOpacity(double opacityPercent)
    {
        OpacityPercent = opacityPercent;
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

    private void ClearPendingDelete()
    {
        foreach (var item in _allItems.Where(item => item.PendingDelete))
        {
            item.PendingDelete = false;
        }

        _pendingDeleteItem = null;
        _deleteHoldTimer.Stop();
    }

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

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = !IsSettingsOpen;
        LogWindowEvent("SettingsButton_Click", $"IsSettingsOpen={IsSettingsOpen}");
    }

    private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = false;
        LogWindowEvent("SettingsCloseButton_Click");
    }

    private void PendingTab_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        _viewMode = ViewMode.Pending;
        RefreshVisibleItems();
        LogWindowEvent("PendingTab_Click");
    }

    private void CompletedTab_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        _viewMode = ViewMode.Completed;
        RefreshVisibleItems();
        LogWindowEvent("CompletedTab_Click");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        var text = DraftText.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            LogWindowEvent("AddButton_Click.SkipEmpty");
            return;
        }

        _allItems.Insert(0, new TodoItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = text,
            NoteDate = DateTime.Today,
            CreatedAt = DateTime.Now
        });
        DraftText = string.Empty;
        SaveTodos();
        RefreshVisibleItems();
        LogWindowEvent("AddButton_Click.Done");
    }

    private void ToggleDoneButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("ToggleDoneButton_Click.InvalidTag");
            return;
        }

        if (item.CompletedAt is null)
        {
            item.CompletedAt = DateTime.Now;
            item.Pinned = false;
        }
        else
        {
            item.CompletedAt = null;
        }
        SaveTodos();
        RefreshVisibleItems();
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

        item.Pinned = !item.Pinned;
        SaveTodos();
        RefreshVisibleItems();
        LogWindowEvent("PinButton_Click.Done", $"ItemId={item.Id},Pinned={item.Pinned}");
    }

    private void TodoItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        if (sender is not Border { Tag: TodoItem item })
        {
            return;
        }

        ClearPendingDelete();
        _pendingDeleteItem = item;
        _deleteHoldTimer.Stop();
        _deleteHoldTimer.Start();
    }

    private void TodoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _deleteHoldTimer.Stop();
        _pendingDeleteItem = null;
    }

    private void TodoItem_MouseLeave(object sender, MouseEventArgs e)
    {
        _deleteHoldTimer.Stop();
        _pendingDeleteItem = null;
    }

    private void DeleteHoldTimer_Tick(object? sender, EventArgs e)
    {
        _deleteHoldTimer.Stop();
        if (_pendingDeleteItem is null)
        {
            LogWindowEvent("DeleteHoldTimer_Tick.Skip");
            return;
        }

        foreach (var item in _allItems.Where(item => item.PendingDelete))
        {
            item.PendingDelete = false;
        }

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

        _allItems.Remove(item);
        SaveTodos();
        RefreshVisibleItems();
        ClearPendingDelete();
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

    private void DraftTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        e.Handled = true;
        AddButton_Click(sender, new RoutedEventArgs());
        LogWindowEvent("DraftTextBox_PreviewKeyDown.Submit");
    }

    private void TodayDateButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void ClearDateButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void CloseDatePopupButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        EnsureInteractiveMode();
        // 发送 WM_NCLBUTTONDOWN + HTBOTTOM，让系统处理底部拖拽 resize
        const int WM_NCLBUTTONDOWN = 0x00A1;
        const int HTBOTTOM = 15;
        SendMessage(_hwnd, WM_NCLBUTTONDOWN, new IntPtr(HTBOTTOM), IntPtr.Zero);
        e.Handled = true;
        LogWindowEvent("ResizeGrip_MouseLeftButtonDown.Handled");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

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

        if (msg == WM_EXITSIZEMOVE && !_hiddenByUser)
        {
            WindowStateManager.SavePosition(Left, Top, Width, Height);
            LogWindowEvent("WndProc.WM_EXITSIZEMOVE");
        }

        return IntPtr.Zero;
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
