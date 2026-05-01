using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyNote;

public partial class MainWindow
{
    // 通过将窗口 parent 到 SysListView32，而不是 WorkerW，
    // 避免在点击"显示桌面"时被 WorkerW 覆盖。
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
        SetWindowPos(_hwnd, HWND_TOP, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
        var curStyle = GetWindowLong(_hwnd, GWL_STYLE);
        SetWindowLong(_hwnd, GWL_STYLE, curStyle | 0x00020000 | 0x00010000);

        var excluded = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref excluded, sizeof(int));
        LogWindowEvent("EnterDesktopMode.Done", $"DesktopHost=0x{desktop.ToInt64():X},DesktopZOrder=Top");
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
        // Temporary topmost lifts the window out of Show Desktop; the next call restores normal z-order.
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        Activate();
        Focus();
        SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
        LogWindowEvent("EnsureInteractiveMode.Done");
    }

    public void HideWindow()
    {
        LogWindowEvent("HideWindow.Before");
        _desktopReentryTimer.Stop();
        _desktopReentryRequiresExternalForeground = false;
        _reenterDesktopOnNextDeactivation = false;
        _hiddenByUser = true;
        PersistWindowPlacement("HideWindow");
        Hide();
        LogWindowEvent("HideWindow.After");
    }

    public void ShowWindow()
    {
        LogWindowEvent("ShowWindow.Before");
        _desktopReentryTimer.Stop();
        _desktopReentryRequiresExternalForeground = false;
        _hiddenByUser = false;
        Show();
        EnsureInteractiveMode();
        ReenterDesktopModeSoon(TrayRestoreDesktopReentryDelay);
        LogWindowEvent("ShowWindow.After");
    }

    public void DockToTopRight()
    {
        LogWindowEvent("DockToTopRight.Start");
        _desktopReentryTimer.Stop();
        _desktopReentryRequiresExternalForeground = false;
        EnsureInteractiveMode();
        if (!ApplyTopRightPlacement())
            return;

        PersistWindowPlacement("DockToTopRight");
        ReenterDesktopModeSoon(TrayRestoreDesktopReentryDelay);
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

    private void ReenterDesktopModeSoon(TimeSpan? delay = null, bool requireExternalForeground = false)
    {
        _desktopReentryTimer.Stop();
        _desktopReentryRequiresExternalForeground = requireExternalForeground;
        if (!requireExternalForeground)
        {
            _reenterDesktopOnNextDeactivation = false;
        }

        if (delay is { } requestedDelay && requestedDelay > TimeSpan.Zero)
        {
            _desktopReentryTimer.Interval = requestedDelay;
            _desktopReentryTimer.Start();
            LogWindowEvent("ReenterDesktopModeSoon.Schedule", $"DelayMs={requestedDelay.TotalMilliseconds:0},RequireExternalForeground={requireExternalForeground}");
            return;
        }

        LogWindowEvent("ReenterDesktopModeSoon.Schedule", $"DelayMs=0,RequireExternalForeground={requireExternalForeground}");
        Dispatcher.BeginInvoke(EnterDesktopMode, DispatcherPriority.ApplicationIdle);
    }

    private void TryCompleteDeferredDesktopReentry()
    {
        if (_desktopReentryRequiresExternalForeground && !IsExternalForegroundWindow(out var foregroundDetails))
        {
            _desktopReentryRequiresExternalForeground = false;
            LogWindowEvent("ReenterDesktopModeSoon.Defer", foregroundDetails);
            return;
        }

        _desktopReentryRequiresExternalForeground = false;
        _reenterDesktopOnNextDeactivation = false;
        EnterDesktopMode();
    }

    private bool IsExternalForegroundWindow(out string details)
    {
        var foreground = GetForegroundWindow();
        var root = foreground == IntPtr.Zero ? IntPtr.Zero : GetAncestor(foreground, GA_ROOT);
        if (root == IntPtr.Zero)
            root = foreground;

        var foregroundClass = GetWindowClassName(foreground);
        var rootClass = GetWindowClassName(root);
        details = $"Foreground=0x{foreground.ToInt64():X},ForegroundClass={foregroundClass},Root=0x{root.ToInt64():X},RootClass={rootClass}";

        if (foreground == IntPtr.Zero || foreground == _hwnd || root == _hwnd)
            return false;

        return !IsShellWindowClass(foregroundClass) && !IsShellWindowClass(rootClass);
    }
}
