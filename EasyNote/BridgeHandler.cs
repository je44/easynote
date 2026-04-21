using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace EasyNote;

internal static class BridgeHandler
{
    public static void Handle(string json, MainWindow window, WebView2 webView)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<BridgeMessage>(json);
            if (msg == null) return;

            object? result = msg.Cmd switch
            {
                "hide_main_window" => HandleHideWindow(window),
                "start_drag" => HandleStartDrag(window),
                "ensure_interactive" => HandleEnsureInteractive(window),
                "resize_height" => HandleResizeHeight(msg, window),
                "set_window_opacity" => HandleSetWindowOpacity(msg, window),
                "set_dragging" => HandleSetDragging(msg, window),
                "dock_main_window" => HandleDock(window),
                "get_desktop_settings" => HandleGetSettings(),
                "set_autostart" => HandleSetAutostart(msg),
                _ => throw new Exception($"Unknown command: {msg.Cmd}")
            };

            SendResponse(webView, msg.Id, true, result, null);
        }
        catch (Exception ex)
        {
            var msg = JsonSerializer.Deserialize<BridgeMessage>(json);
            SendResponse(webView, msg?.Id ?? "", false, null, ex.Message);
        }
    }

    private static object? HandleHideWindow(MainWindow window)
    {
        window.Dispatcher.Invoke(() => window.HideWindow());
        return null;
    }

    private static object? HandleStartDrag(MainWindow window)
    {
        window.Dispatcher.Invoke(() =>
        {
            try { window.BeginDrag(); }
            catch { }
        });
        return null;
    }

    private static object? HandleEnsureInteractive(MainWindow window)
    {
        window.Dispatcher.Invoke(window.EnsureInteractiveMode);
        return null;
    }

    private static object? HandleResizeHeight(BridgeMessage msg, MainWindow window)
    {
        if (msg.Args?.TryGetProperty("height", out var heightProp) == true)
        {
            window.Dispatcher.Invoke(() => window.ResizeHeight(heightProp.GetDouble()));
        }
        return null;
    }

    private static object? HandleSetWindowOpacity(BridgeMessage msg, MainWindow window)
    {
        if (msg.Args?.TryGetProperty("opacity", out var opacityProp) == true)
        {
            window.Dispatcher.Invoke(() => window.SetWindowOpacity(opacityProp.GetDouble()));
        }
        return null;
    }

    private static object? HandleSetDragging(BridgeMessage msg, MainWindow window)
    {
        // Tauri 兼容：前端调用 set_dragging(true) 后会调用 startDragging()
        // WPF 中拖动是同步的，这里不需要额外处理
        return null;
    }

    private static object? HandleDock(MainWindow window)
    {
        window.Dispatcher.Invoke(() => window.DockToTopRight());
        return null;
    }

    private static object HandleGetSettings()
    {
        return new
        {
            autostart_enabled = IsAutostartEnabled(),
            is_windows = true
        };
    }

    private static object? HandleSetAutostart(BridgeMessage msg)
    {
        if (msg.Args?.TryGetProperty("enabled", out var enabledProp) == true)
        {
            SetAutostart(enabledProp.GetBoolean());
        }
        return null;
    }

    private static void SendResponse(WebView2 webView, string id, bool ok, object? result, string? error)
    {
        var response = new
        {
            type = "response",
            id,
            ok,
            result,
            error
        };
        var json = JsonSerializer.Serialize(response);
        webView.CoreWebView2.PostWebMessageAsString(json);
    }

    private static bool IsAutostartEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue("DesktopMemoBoard") != null;
    }

    private static void SetAutostart(bool enable)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key == null) return;
        if (enable)
        {
            var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            key.SetValue("DesktopMemoBoard", $"\"{exe}\" --autostart");
        }
        else
        {
            key.DeleteValue("DesktopMemoBoard", false);
        }
    }

    private record BridgeMessage(string Id, string Cmd, JsonElement? Args);
}
