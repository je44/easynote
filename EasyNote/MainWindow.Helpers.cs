using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyNote;

public partial class MainWindow
{
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

    private void NotifyDraftPanelStateChanged()
    {
        OnPropertyChanged(nameof(IsDraftEditMode));
        OnPropertyChanged(nameof(DraftPrimaryActionText));
        OnPropertyChanged(nameof(DraftPanelStatusText));
        OnPropertyChanged(nameof(HasDraftText));
    }

    private bool CanToggleDoneFromCurrentView(TodoItem item)
        => IsPendingView ? item.CompletedAt is null : item.CompletedAt is not null;

    public void PersistWindowPlacement(string reason)
    {
        var saved = WindowStateManager.SavePosition(this);
        LogWindowEvent("PersistWindowPlacement", $"Reason={reason},Saved={saved}");
    }

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

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
