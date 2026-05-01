namespace EasyNote;

public partial class MainWindow
{
    internal TodoItem AddTodoForAutomation(string text)
    {
        if (!TryAddTodo(text, out var item) || item is null)
            throw new InvalidOperationException("Unable to add todo item.");

        return item;
    }

    internal void SetViewModeForAutomation(ViewMode viewMode) => SwitchView(viewMode);

    internal void ToggleDoneForAutomation(TodoItem item) => ToggleDone(item);

    internal void TogglePinnedForAutomation(TodoItem item) => TogglePinned(item);

    internal void BeginEditForAutomation(TodoItem item) => BeginTodoEdit(item);

    internal bool CommitEditForAutomation(TodoItem item, string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        item.Text = normalized;
        item.EditingText = normalized;
        item.IsEditing = false;
        _editingTodoItem = null;
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        SaveTodos();
        RefreshVisibleItems();
        NotifyDraftPanelStateChanged();
        return true;
    }

    internal void CancelEditForAutomation(TodoItem item) => CancelTodoEdit(item);

    internal void MarkPendingDeleteForAutomation(TodoItem item)
    {
        ClearPendingDelete();
        _pendingDeleteItem = item;
        DeleteHoldTimer_Tick(this, EventArgs.Empty);
    }

    internal void CancelPendingDeleteForAutomation() => ClearPendingDelete();

    internal void DeleteTodoForAutomation(TodoItem item) => DeleteTodo(item);

    internal IReadOnlyList<TodoItem> SnapshotItemsForAutomation() => _allItems.ToList();

    internal TodoItem? FindTodoForAutomation(string id) => _allItems.FirstOrDefault(item => item.Id == id);

    internal bool TriggerHotkeyForAutomation()
        => _hwnd != IntPtr.Zero && PostMessage(_hwnd, WM_HOTKEY, new IntPtr(HOTKEY_SHOW), IntPtr.Zero);

    internal bool IsTopLevelWindowForAutomation()
        => _hwnd != IntPtr.Zero && GetWindowLongPtr(_hwnd, GWLP_HWNDPARENT) == IntPtr.Zero;

    internal bool IsDesktopHostedWindowForAutomation()
        => _hwnd != IntPtr.Zero && GetWindowLongPtr(_hwnd, GWLP_HWNDPARENT) != IntPtr.Zero;

    internal bool IsTopmostWindowForAutomation()
        => _hwnd != IntPtr.Zero && (GetWindowLong(_hwnd, GWL_EXSTYLE) & 0x00000008) != 0;

    internal void CompleteDeferredDesktopReentryForAutomation()
    {
        _desktopReentryTimer.Stop();
        _desktopReentryRequiresExternalForeground = false;
        _reenterDesktopOnNextDeactivation = false;
        ReenterDesktopModeSoon();
    }

    internal void ScheduleExternalForegroundDesktopReentryForAutomation()
    {
        _reenterDesktopOnNextDeactivation = true;
        ReenterDesktopModeSoon(ExternalWindowDesktopReentryDelay, requireExternalForeground: true);
    }
}
