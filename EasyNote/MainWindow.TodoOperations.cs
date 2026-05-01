using System.Windows;
using System.Windows.Input;

namespace EasyNote;

public partial class MainWindow
{
    private void LoadTodos()
    {
        LogWindowEvent("LoadTodos.Start");
        try
        {
            var items = LocalUserDataStore.ReadJson<List<TodoItem>>(TodoStatePath);
            _allItems.Clear();
            foreach (var item in items ?? [])
            {
                _allItems.Add(item);
            }
        }
        catch
        {
            _allItems.Clear();
            LogWindowEvent("LoadTodos.Error");
        }

        RefreshVisibleItems();
        LogWindowEvent("LoadTodos.Done", $"ItemCount={_allItems.Count}");
    }

    private void SaveTodos()
    {
        try
        {
            LocalUserDataStore.WriteJson(TodoStatePath, _allItems);
            LogWindowEvent("SaveTodos.Done", $"ItemCount={_allItems.Count}");
        }
        catch
        {
            LogWindowEvent("SaveTodos.Error");
        }
    }

    private bool TryAddTodo(string text, out TodoItem? item)
    {
        ClearPendingDelete();
        ClearTodoActions();
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            item = null;
            return false;
        }

        item = new TodoItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Text = normalized,
            CreatedAt = DateTime.Now,
            IsNew = true
        };

        _allItems.Insert(0, item);
        DraftText = string.Empty;
        SaveTodos();
        RefreshVisibleItems();
        ClearNewItemFlagAfterEntrance(item);
        return true;
    }

    private async void ClearNewItemFlagAfterEntrance(TodoItem item)
    {
        await Task.Delay(420);
        if (_allItems.Contains(item))
        {
            item.IsNew = false;
        }
    }

    private void ToggleDone(TodoItem item)
    {
        ClearPendingDelete();
        if (item.CompletedAt is null)
        {
            item.CompletedAt = DateTime.Now;
            item.Pinned = false;
        }
        else
        {
            item.CompletedAt = null;
        }

        item.ActionOpen = false;
        item.Completing = false;
        item.SuppressActionOpenAnimation = false;
        SaveTodos();
        RefreshVisibleItems();
    }

    private void TogglePinned(TodoItem item)
    {
        ClearPendingDelete();
        ClearTodoActions(item);
        item.Pinned = !item.Pinned;
        SaveTodos();
        RefreshVisibleItems();
    }

    private void DeleteTodo(TodoItem item)
    {
        _allItems.Remove(item);
        SaveTodos();
        RefreshVisibleItems();
        ClearPendingDelete();
    }

    private void BeginTodoEdit(TodoItem item)
    {
        ClearPendingDelete();
        CancelPendingActionToggle();
        ClearTodoActions(item);
        ClearTodoEditing(item);
        _addDraftBuffer = DraftText;

        if (_editingTodoItem is not null && _editingTodoItem != item)
        {
            _editingTodoItem.EditingText = _editingTodoItem.Text;
            _editingTodoItem.IsEditing = false;
        }

        _editingTodoItem = item;
        item.ActionOpen = false;
        item.Completing = false;
        item.SuppressActionOpenAnimation = false;
        item.EditingText = item.Text;
        item.IsEditing = true;
        DraftText = item.Text;
        IsDraftOpen = true;
        IsSettingsOpen = false;
        NotifyDraftPanelStateChanged();
        FocusDraftInput(selectAll: true);
    }

    private bool CommitTodoEdit(TodoItem item)
    {
        var normalized = DraftText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            LogWindowEvent("CommitTodoEdit.SkipEmpty", $"ItemId={item.Id}");
            return false;
        }

        ClearPendingDelete();
        CancelPendingActionToggle();
        item.Text = normalized;
        item.EditingText = normalized;
        item.IsEditing = false;
        _editingTodoItem = null;
        SaveTodos();
        RefreshVisibleItems();
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("CommitTodoEdit.Done", $"ItemId={item.Id}");
        return true;
    }

    private void CancelTodoEdit(TodoItem? item)
    {
        CancelPendingActionToggle();
        if (item is null)
        {
            CloseDraftPanel();
            return;
        }

        item.EditingText = item.Text;
        item.IsEditing = false;
        _editingTodoItem = null;
        DraftText = _addDraftBuffer;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("CancelTodoEdit.Done", $"ItemId={item.Id}");
    }

    private void CloseDraftPanel()
    {
        if (_editingTodoItem is not null)
        {
            _editingTodoItem.EditingText = _editingTodoItem.Text;
            _editingTodoItem.IsEditing = false;
            _editingTodoItem = null;
        }

        IsDraftOpen = false;
        DraftText = _addDraftBuffer;
        NotifyDraftPanelStateChanged();
    }

    private void SwitchView(ViewMode viewMode)
    {
        CancelPendingActionToggle();
        foreach (var item in _allItems.Where(item => item.ActionOpen))
        {
            item.SuppressActionOpenAnimation = true;
        }

        _viewMode = viewMode;
        RefreshVisibleItems();
    }

    private void RefreshVisibleItems()
    {
        VisibleItems.Clear();

        IEnumerable<TodoItem> items = _viewMode switch
        {
            ViewMode.Pending => _allItems
                .Where(item => item.CompletedAt is null)
                .OrderByDescending(item => item.Pinned)
                .ThenByDescending(item => item.CreatedAt),
            _ => _allItems
                .Where(item => item.CompletedAt is not null)
                .OrderByDescending(item => item.CompletedAt)
        };

        foreach (var item in items)
        {
            VisibleItems.Add(item);
        }

        OnPropertyChanged(nameof(HasVisibleItems));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(IsPendingView));
        OnPropertyChanged(nameof(IsDraftPanelActiveInCurrentView));
        OnPropertyChanged(nameof(IsSettingsPanelActiveInCurrentView));
        OnPropertyChanged(nameof(PendingTabTag));
        OnPropertyChanged(nameof(CompletedTabTag));
        OnPropertyChanged(nameof(DeleteOverlayText));
        LogWindowEvent("RefreshVisibleItems", $"All={_allItems.Count},Visible={VisibleItems.Count},ViewMode={_viewMode}");
    }

    private void FocusDraftInput(bool selectAll)
    {
        Dispatcher.BeginInvoke(() =>
        {
            DraftInputTextBox.Focus();
            Keyboard.Focus(DraftInputTextBox);
            if (selectAll)
            {
                DraftInputTextBox.SelectAll();
            }
            else
            {
                DraftInputTextBox.CaretIndex = DraftInputTextBox.Text.Length;
            }
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
}
