using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
        SessionEnding += OnSessionEnding;

        _mainWindow = new MainWindow();
        _mainWindow.Show();

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMain();

        var autostartItem = FindAutostartMenuItem();
        if (autostartItem != null)
            autostartItem.IsChecked = IsAutostartEnabled();

        WindowEventLogger.Write("App", "OnStartup.Done");

        var selfTestOutputPath = TryGetSelfTestOutputPath(e.Args);
        if (selfTestOutputPath != null)
            Dispatcher.BeginInvoke(async () => await RunSelfTestAsync(selfTestOutputPath), DispatcherPriority.ApplicationIdle);
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
        _mainWindow?.PersistWindowPlacement("TrayQuit");
        DisposeTrayIcon();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DisposeTrayIcon();
        base.OnExit(e);
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

    private void OnSessionEnding(object? sender, SessionEndingCancelEventArgs e)
    {
        WindowEventLogger.Write("App", $"OnSessionEnding | reason={e.ReasonSessionEnding}");
        _mainWindow?.PersistWindowPlacement("SessionEnding");
    }

    private void DisposeTrayIcon()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private static string? TryGetSelfTestOutputPath(string[] args)
    {
        if (!args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
            return null;

        const string prefix = "--self-test-output=";
        var explicitOutput = args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (explicitOutput != null)
            return explicitOutput[prefix.Length..];

        return System.IO.Path.Combine(AppPaths.AppDataDirectory, "self-test-report.json");
    }

    private async Task RunSelfTestAsync(string outputPath)
    {
        var report = new SelfTestReport();
        var todosPath = System.IO.Path.Combine(AppPaths.AppDataDirectory, "todos.json");
        var statePath = System.IO.Path.Combine(AppPaths.AppDataDirectory, "window-state.json");
        var originalTodos = System.IO.File.Exists(todosPath) ? await System.IO.File.ReadAllTextAsync(todosPath) : null;
        var originalState = System.IO.File.Exists(statePath) ? await System.IO.File.ReadAllTextAsync(statePath) : null;
        var originalAutostart = IsAutostartEnabled();

        try
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("Main window not available for self-test.");

            report.Checks.Add(new SelfTestCheck
            {
                Name = "startup",
                Passed = _mainWindow.IsLoaded && _trayIcon != null,
                Details = $"loaded={_mainWindow.IsLoaded}, trayIconCreated={_trayIcon != null}"
            });

            report.Checks.Add(new SelfTestCheck
            {
                Name = "autostart-menu",
                Passed = FindAutostartMenuItem() != null,
                Details = $"menuFound={FindAutostartMenuItem() != null}"
            });

            _mainWindow.SetViewModeForAutomation(ViewMode.Pending);
            foreach (var item in _mainWindow.SnapshotItemsForAutomation().ToList())
                _mainWindow.DeleteTodoForAutomation(item);

            var added = _mainWindow.AddTodoForAutomation("self-test pending item");
            report.Checks.Add(new SelfTestCheck
            {
                Name = "add-todo",
                Passed = _mainWindow.FindTodoForAutomation(added.Id) != null,
                Details = $"addedId={added.Id}"
            });
            report.Checks.Add(new SelfTestCheck
            {
                Name = "todo-auto-save",
                Passed = (await System.IO.File.ReadAllTextAsync(todosPath)).Contains(added.Id, StringComparison.Ordinal),
                Details = $"savedToDisk={(await System.IO.File.ReadAllTextAsync(todosPath)).Contains(added.Id, StringComparison.Ordinal)}"
            });

            _mainWindow.BeginEditForAutomation(added);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "edit-state-visible",
                Passed = _mainWindow.FindTodoForAutomation(added.Id)?.IsEditing == true,
                Details = $"isEditing={_mainWindow.FindTodoForAutomation(added.Id)?.IsEditing}"
            });

            var editSaved = _mainWindow.CommitEditForAutomation(added, "self-test edited item");
            report.Checks.Add(new SelfTestCheck
            {
                Name = "edit-todo",
                Passed = editSaved && _mainWindow.FindTodoForAutomation(added.Id)?.Text == "self-test edited item",
                Details = $"editSaved={editSaved}, text={_mainWindow.FindTodoForAutomation(added.Id)?.Text}"
            });

            _mainWindow.BeginEditForAutomation(added);
            _mainWindow.FindTodoForAutomation(added.Id)!.EditingText = "self-test canceled edit";
            _mainWindow.CancelEditForAutomation(added);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "cancel-edit",
                Passed = _mainWindow.FindTodoForAutomation(added.Id)?.IsEditing == false
                    && _mainWindow.FindTodoForAutomation(added.Id)?.Text == "self-test edited item",
                Details = $"isEditing={_mainWindow.FindTodoForAutomation(added.Id)?.IsEditing}, text={_mainWindow.FindTodoForAutomation(added.Id)?.Text}"
            });

            _mainWindow.TogglePinnedForAutomation(added);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "pin-todo",
                Passed = _mainWindow.FindTodoForAutomation(added.Id)?.Pinned == true,
                Details = $"pinned={_mainWindow.FindTodoForAutomation(added.Id)?.Pinned}"
            });

            _mainWindow.ToggleDoneForAutomation(added);
            var completed = _mainWindow.FindTodoForAutomation(added.Id);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "complete-todo",
                Passed = completed?.CompletedAt != null && completed.Pinned == false,
                Details = $"completedAt={completed?.CompletedAt:O}, pinned={completed?.Pinned}"
            });

            _mainWindow.SetViewModeForAutomation(ViewMode.Completed);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "completed-view",
                Passed = _mainWindow.VisibleItems.Any(item => item.Id == added.Id),
                Details = $"visibleCount={_mainWindow.VisibleItems.Count}"
            });

            _mainWindow.ToggleDoneForAutomation(completed!);
            _mainWindow.SetViewModeForAutomation(ViewMode.Pending);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "restore-todo",
                Passed = _mainWindow.VisibleItems.Any(item => item.Id == added.Id) && _mainWindow.FindTodoForAutomation(added.Id)?.CompletedAt == null,
                Details = $"visibleInPending={_mainWindow.VisibleItems.Any(item => item.Id == added.Id)}"
            });

            _mainWindow.MarkPendingDeleteForAutomation(added);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "arm-delete",
                Passed = _mainWindow.FindTodoForAutomation(added.Id)?.PendingDelete == true,
                Details = $"pendingDelete={_mainWindow.FindTodoForAutomation(added.Id)?.PendingDelete}"
            });

            _mainWindow.CancelPendingDeleteForAutomation();
            report.Checks.Add(new SelfTestCheck
            {
                Name = "cancel-delete",
                Passed = _mainWindow.FindTodoForAutomation(added.Id)?.PendingDelete == false,
                Details = $"pendingDelete={_mainWindow.FindTodoForAutomation(added.Id)?.PendingDelete}"
            });

            _mainWindow.MarkPendingDeleteForAutomation(added);
            _mainWindow.DeleteTodoForAutomation(added);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "delete-todo",
                Passed = _mainWindow.FindTodoForAutomation(added.Id) == null,
                Details = $"remainingCount={_mainWindow.SnapshotItemsForAutomation().Count}"
            });

            _mainWindow.HideWindow();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var hiddenPassed = !_mainWindow.IsVisible;
            _mainWindow.ShowWindow();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "hide-show",
                Passed = hiddenPassed && _mainWindow.IsVisible,
                Details = $"hiddenPassed={hiddenPassed}, visibleAfterShow={_mainWindow.IsVisible}"
            });
            _mainWindow.CompleteDeferredDesktopReentryForAutomation();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            _mainWindow.HideWindow();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            OnTrayShow(this, new RoutedEventArgs());
            await Task.Delay(900);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "tray-show",
                Passed = _mainWindow.IsVisible && _mainWindow.IsDesktopHostedWindowForAutomation() && !_mainWindow.IsTopmostWindowForAutomation(),
                Details = $"visibleAfterTrayShow={_mainWindow.IsVisible}, desktopHosted={_mainWindow.IsDesktopHostedWindowForAutomation()}, topMost={_mainWindow.IsTopmostWindowForAutomation()}"
            });
            await Task.Delay(450);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "tray-show-stays-desktop-hosted",
                Passed = _mainWindow.IsVisible && _mainWindow.IsDesktopHostedWindowForAutomation() && !_mainWindow.IsTopmostWindowForAutomation(),
                Details = $"visible={_mainWindow.IsVisible}, desktopHosted={_mainWindow.IsDesktopHostedWindowForAutomation()}, topMost={_mainWindow.IsTopmostWindowForAutomation()}"
            });
            _mainWindow.CompleteDeferredDesktopReentryForAutomation();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            var hotkeyTriggered = _mainWindow.TriggerHotkeyForAutomation();
            await Task.Delay(200);
            var hotkeyHidden = !_mainWindow.IsVisible;
            _mainWindow.TriggerHotkeyForAutomation();
            await Task.Delay(200);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "global-hotkey",
                Passed = hotkeyTriggered && hotkeyHidden && _mainWindow.IsVisible,
                Details = $"postMessage={hotkeyTriggered}, hiddenAfterHotkey={hotkeyHidden}, visibleAfterSecondHotkey={_mainWindow.IsVisible}"
            });
            _mainWindow.CompleteDeferredDesktopReentryForAutomation();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            var originalTop = _mainWindow.Top;
            _mainWindow.DockToTopRight();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var dockedNearTopMargin = Math.Abs(_mainWindow.Top - 20) < 2;
            report.Checks.Add(new SelfTestCheck
            {
                Name = "dock-top-right",
                Passed = _mainWindow.Top <= originalTop || dockedNearTopMargin,
                Details = $"topBefore={originalTop:0.##}, topAfter={_mainWindow.Top:0.##}, nearTopMargin={dockedNearTopMargin}, leftAfter={_mainWindow.Left:0.##}"
            });

            var trayDockTopBefore = _mainWindow.Top;
            OnTrayDock(this, new RoutedEventArgs());
            await Task.Delay(900);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "tray-dock",
                Passed = _mainWindow.IsVisible && _mainWindow.Top <= trayDockTopBefore && _mainWindow.IsDesktopHostedWindowForAutomation() && !_mainWindow.IsTopmostWindowForAutomation(),
                Details = $"visible={_mainWindow.IsVisible}, desktopHosted={_mainWindow.IsDesktopHostedWindowForAutomation()}, topMost={_mainWindow.IsTopmostWindowForAutomation()}, topBefore={trayDockTopBefore:0.##}, topAfter={_mainWindow.Top:0.##}"
            });
            _mainWindow.CompleteDeferredDesktopReentryForAutomation();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

            _mainWindow.OpacityPercent = 61;
            _mainWindow.Left = 321;
            _mainWindow.Top = 123;
            _mainWindow.Height = 611;
            _mainWindow.PersistWindowPlacement("SelfTest");

            var restoredWindow = new MainWindow();
            WindowStateManager.RestorePosition(restoredWindow);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "window-state-persistence",
                Passed = Math.Abs(restoredWindow.Left - 321) < 1 && Math.Abs(restoredWindow.Top - 123) < 1 && Math.Abs(restoredWindow.Height - 611) < 1,
                Details = $"left={restoredWindow.Left:0.##}, top={restoredWindow.Top:0.##}, height={restoredWindow.Height:0.##}"
            });
            report.Checks.Add(new SelfTestCheck
            {
                Name = "opacity-persistence",
                Passed = Math.Abs(restoredWindow.OpacityPercent - 61) < 0.1,
                Details = $"opacity={restoredWindow.OpacityPercent:0.##}"
            });

            var autostartBefore = IsAutostartEnabled();
            SetAutostart(!autostartBefore);
            var autostartToggled = IsAutostartEnabled() == !autostartBefore;
            SetAutostart(autostartBefore);
            report.Checks.Add(new SelfTestCheck
            {
                Name = "autostart-toggle",
                Passed = autostartToggled && IsAutostartEnabled() == autostartBefore,
                Details = $"before={autostartBefore}, toggled={autostartToggled}, restored={IsAutostartEnabled() == autostartBefore}"
            });
        }
        catch (Exception ex)
        {
            report.Checks.Add(new SelfTestCheck
            {
                Name = "self-test-runner",
                Passed = false,
                Details = ex.ToString()
            });
        }
        finally
        {
            await RestoreFileAsync(todosPath, originalTodos);
            await RestoreFileAsync(statePath, originalState);
            SetAutostart(originalAutostart);
        }

        var outputDirectory = System.IO.Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            System.IO.Directory.CreateDirectory(outputDirectory);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(outputPath, json);

        Environment.ExitCode = report.Success ? 0 : 1;
        Shutdown();
    }

    private static async Task RestoreFileAsync(string path, string? originalContent)
    {
        if (originalContent == null)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            return;
        }

        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            System.IO.Directory.CreateDirectory(directory);

        await System.IO.File.WriteAllTextAsync(path, originalContent);
    }
}
