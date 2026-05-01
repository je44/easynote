using System.Text.Json.Serialization;
using System.Windows;

namespace EasyNote;

internal sealed class SavedWindowPlacement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double? OpacityPercent { get; set; }
    public bool? IsNightTheme { get; set; }

    [JsonConstructor]
    public SavedWindowPlacement()
    {
    }

    public SavedWindowPlacement(double x, double y, double width, double height, double? opacityPercent = null, bool? isNightTheme = null)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        OpacityPercent = opacityPercent;
        IsNightTheme = isNightTheme;
    }
}

internal static class WindowStateManager
{
    private static readonly string StatePath = LocalUserDataStore.WindowStatePath;

    private const double MaxWindowWidth = 800;
    private const double MaxWindowHeight = 1020;

    public static bool SavePosition(Window window)
    {
        if (!TryBuildPlacement(window, out var placement))
            return false;

        return SavePosition(placement.X, placement.Y, placement.Width, placement.Height, placement.OpacityPercent, placement.IsNightTheme);
    }

    public static bool SavePosition(double x, double y, double width, double height, double? opacityPercent = null, bool? isNightTheme = null)
    {
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height) || width <= 0 || height <= 0)
            return false;

        if (opacityPercent is double opacity && (!IsFinite(opacity) || opacity < 0))
            return false;

        try
        {
            LocalUserDataStore.WriteJson(StatePath, new SavedWindowPlacement(x, y, width, height, opacityPercent, isNightTheme));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool RestorePosition(Window window)
    {
        try
        {
            if (!TryReadPlacement(out var state))
                return false;

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

            if (window is MainWindow mainWindow && state.OpacityPercent is double opacityPercent)
                mainWindow.OpacityPercent = opacityPercent;

            if (window is MainWindow themedWindow && state.IsNightTheme is bool isNightTheme)
                themedWindow.IsNightTheme = isNightTheme;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadPlacement(out SavedWindowPlacement placement)
    {
        placement = default!;
        var state = LocalUserDataStore.ReadJson<SavedWindowPlacement>(StatePath);
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

        double? opacityPercent = window is MainWindow mainWindow ? mainWindow.OpacityPercent : null;
        bool? isNightTheme = window is MainWindow themedWindow ? themedWindow.IsNightTheme : null;
        placement = new SavedWindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, opacityPercent, isNightTheme);
        return true;
    }

    private static bool IsFinite(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);
}
