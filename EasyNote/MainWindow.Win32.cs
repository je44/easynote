using System.Runtime.InteropServices;
using System.Text;

namespace EasyNote;

public partial class MainWindow
{
    #region Win32 Imports

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
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    #endregion

    #region Win32 Structs

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

    #endregion

    #region Win32 Constants

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
    private static readonly IntPtr HWND_TOP = new(0);
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
    private const int HOTKEY_SHOW = 1;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const int WM_HOTKEY = 0x0312;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MINIMIZE = 0xF020;
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const uint GA_ROOT = 2;

    #endregion

    private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLongPtr32(hwnd, index));

    private static string GetWindowClassName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return string.Empty;

        var buffer = new StringBuilder(256);
        var length = GetClassName(hwnd, buffer, buffer.Capacity);
        return length > 0 ? buffer.ToString() : string.Empty;
    }

    private static bool IsShellWindowClass(string className)
        => className is "Progman"
            or "WorkerW"
            or "SHELLDLL_DefView"
            or "SysListView32"
            or "Shell_TrayWnd"
            or "NotifyIconOverflowWindow"
            or "DV2ControlHost"
            or "#32768";
}
