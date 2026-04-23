using System.IO;
using System.Text.Json;
using System.Windows;

namespace EasyNote;

internal record SavedWindowPlacement(double X, double Y, double Width, double Height);

internal static class WindowStateManager
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "easy-note", "window-state.json");

    private const double MaxWindowWidth = 800;
    private const double MaxWindowHeight = 920;

    public static bool SavePosition(Window window)
    {
        if (!TryBuildPlacement(window, out var placement))
            return false;

        return SavePosition(placement.X, placement.Y, placement.Width, placement.Height);
    }

    public static bool SavePosition(double x, double y, double width, double height)
    {
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height) || width <= 0 || height <= 0)
            return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            var json = JsonSerializer.Serialize(new SavedWindowPlacement(x, y, width, height));
            File.WriteAllText(StatePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void RestorePosition(Window window)
    {
        try
        {
            if (!TryReadPlacement(out var state))
                return;

            var screenW = Math.Max(1d, SystemParameters.VirtualScreenWidth);
            var screenH = Math.Max(1d, SystemParameters.VirtualScreenHeight);
            var screenX = SystemParameters.VirtualScreenLeft;
            var screenY = SystemParameters.VirtualScreenTop;

            var width = Math.Clamp(state.Width, window.MinWidth, Math.Max(window.MinWidth, Math.Min(MaxWindowWidth, screenW)));
            var height = Math.Clamp(state.Height, window.MinHeight, Math.Max(window.MinHeight, Math.Min(MaxWindowHeight, screenH)));
            var maxX = Math.Max(screenX, screenX + screenW - width);
            var maxY = Math.Max(screenY, screenY + screenH - height);

            window.Left = Math.Clamp(state.X, screenX, maxX);
            window.Top = Math.Clamp(state.Y, screenY, maxY);
            window.Width = width;
            window.Height = height;
        }
        catch
        {
        }
    }

    private static bool TryReadPlacement(out SavedWindowPlacement placement)
    {
        placement = default!;
        if (!File.Exists(StatePath))
            return false;

        var state = JsonSerializer.Deserialize<SavedWindowPlacement>(File.ReadAllText(StatePath));
        if (state == null)
            return false;

        if (!IsFinite(state.X) || !IsFinite(state.Y) || !IsFinite(state.Width) || !IsFinite(state.Height))
            return false;

        if (state.Width <= 0 || state.Height <= 0)
            return false;

        placement = state;
        return true;
    }

    private static bool TryBuildPlacement(Window window, out SavedWindowPlacement placement)
    {
        placement = default!;

        var bounds = window.WindowState == System.Windows.WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;

        if (bounds.IsEmpty)
            return false;

        if (!IsFinite(bounds.Left) || !IsFinite(bounds.Top) || !IsFinite(bounds.Width) || !IsFinite(bounds.Height))
            return false;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return false;

        placement = new SavedWindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        return true;
    }

    private static bool IsFinite(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);
}
