using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace EasyNote;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;
    private TaskbarIcon? _trayIcon;

    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRunName = "DesktopMemoBoard";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        WindowEventLogger.Write("App", $"OnStartup | args={string.Join(' ', e.Args)} | log={WindowEventLogger.CurrentLogPath}");

        DispatcherUnhandledException += (_, args) =>
            WindowEventLogger.Write("App", $"DispatcherUnhandledException | {args.Exception}");
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            WindowEventLogger.Write("App", $"UnhandledException | {args.ExceptionObject}");

        _mainWindow = new MainWindow();
        _mainWindow.Show();

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMain();

        var autostartItem = FindAutostartMenuItem();
        if (autostartItem != null)
            autostartItem.IsChecked = IsAutostartEnabled();

        WindowEventLogger.Write("App", "OnStartup.Done");
    }

    private void ShowMain()
    {
        WindowEventLogger.Write("App", "ShowMain");
        _mainWindow?.ShowWindow();
    }

    private void OnTrayShow(object sender, RoutedEventArgs e) => ShowMain();

    private void OnTrayDock(object sender, RoutedEventArgs e)
    {
        WindowEventLogger.Write("App", "OnTrayDock");
        ShowMain();
        _mainWindow?.DockToTopRight();
    }

    private void OnTrayAutostart(object sender, RoutedEventArgs e)
    {
        var item = (System.Windows.Controls.MenuItem)sender;
        SetAutostart(item.IsChecked);
        item.IsChecked = IsAutostartEnabled();
        WindowEventLogger.Write("App", $"OnTrayAutostart | enabled={item.IsChecked}");
    }

    private void OnTrayQuit(object sender, RoutedEventArgs e)
    {
        WindowEventLogger.Write("App", "OnTrayQuit");
        _trayIcon?.Dispose();
        Shutdown();
    }

    private System.Windows.Controls.MenuItem? FindAutostartMenuItem()
    {
        var menu = _trayIcon?.ContextMenu;
        if (menu == null) return null;
        foreach (var item in menu.Items)
            if (item is System.Windows.Controls.MenuItem m && m.Name == "AutostartMenuItem")
                return m;
        return null;
    }

    private static bool IsAutostartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppRunName) != null;
    }

    private static void SetAutostart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key == null) return;
        if (enable)
        {
            var exe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            key.SetValue(AppRunName, $"\"{exe}\" --autostart");
        }
        else
        {
            key.DeleteValue(AppRunName, false);
        }
    }
}
