using System.IO;
using System.Text.Json;
using System.Windows;

namespace EasyNote;

internal record WindowState(double X, double Y, double Width, double Height);

internal static class WindowStateManager
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "easy-note", "window-state.json");

    public static void SavePosition(double x, double y, double width, double height)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            var json = JsonSerializer.Serialize(new WindowState(x, y, width, height));
            File.WriteAllText(StatePath, json);
        }
        catch { }
    }

    public static void RestorePosition(Window window)
    {
        try
        {
            if (!File.Exists(StatePath)) return;
            var state = JsonSerializer.Deserialize<WindowState>(File.ReadAllText(StatePath));
            if (state == null) return;

            // 确保位置在屏幕内
            var screenW = SystemParameters.VirtualScreenWidth;
            var screenH = SystemParameters.VirtualScreenHeight;
            var screenX = SystemParameters.VirtualScreenLeft;
            var screenY = SystemParameters.VirtualScreenTop;

            var x = Math.Clamp(state.X, screenX, screenX + screenW - state.Width);
            var y = Math.Clamp(state.Y, screenY, screenY + screenH - state.Height);

            window.Left = x;
            window.Top = y;
            window.Width = Math.Clamp(state.Width, window.MinWidth, 800);
            window.Height = Math.Clamp(state.Height, window.MinHeight, 920);
        }
        catch { }
    }
}
