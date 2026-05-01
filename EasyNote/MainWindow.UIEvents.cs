using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace EasyNote;

public partial class MainWindow
{
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        BeginDrag();
        e.Handled = true;
        LogWindowEvent("Header_MouseLeftButtonDown.Handled");
    }

    private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || e.Handled)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source && IsInteractiveChromeSource(source))
        {
            return;
        }

        BeginDrag();
        e.Handled = true;
        LogWindowEvent("Chrome_MouseLeftButtonDown.Handled");
    }

    private static bool IsInteractiveChromeSource(DependencyObject source)
        => FindParent<Button>(source) is not null
            || FindParent<TextBox>(source) is not null
            || FindParent<Slider>(source) is not null
            || FindParent<ScrollBar>(source) is not null
            || FindParent<ListBoxItem>(source) is not null;

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = !IsSettingsOpen;
        if (IsSettingsOpen)
        {
            CloseDraftPanel();
        }
        LogWindowEvent("SettingsButton_Click", $"IsSettingsOpen={IsSettingsOpen}");
    }

    private void AddTodoToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        ClearTodoActions();

        if (IsDraftEditMode)
        {
            CancelTodoEdit(_editingTodoItem);
        }

        IsDraftOpen = !IsDraftOpen;
        if (IsDraftOpen)
        {
            IsSettingsOpen = false;
            DraftText = _addDraftBuffer;
            FocusDraftInput(selectAll: string.IsNullOrWhiteSpace(DraftText) is false);
        }
        LogWindowEvent("AddTodoToggleButton_Click", $"IsDraftOpen={IsDraftOpen}");
    }

    private void DraftInputTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ClearPendingDelete();
        ClearTodoActions();
        ClearTodoEditing(_editingTodoItem);
        LogWindowEvent("DraftInputTextBox_GotKeyboardFocus");
    }

    private void SettingsCloseButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        IsSettingsOpen = false;
        LogWindowEvent("SettingsCloseButton_Click");
    }

    private void PendingTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(ViewMode.Pending);
        LogWindowEvent("PendingTab_Click");
    }

    private void CompletedTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(ViewMode.Completed);
        LogWindowEvent("CompletedTab_Click");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsDraftEditMode)
        {
            if (_editingTodoItem is null || !CommitTodoEdit(_editingTodoItem))
            {
                LogWindowEvent("AddButton_Click.SkipEditEmpty");
                return;
            }

            LogWindowEvent("AddButton_Click.EditDone");
            return;
        }

        if (!TryAddTodo(DraftText, out _))
        {
            LogWindowEvent("AddButton_Click.SkipEmpty");
            return;
        }

        _addDraftBuffer = string.Empty;
        DraftText = string.Empty;
        IsDraftOpen = false;
        NotifyDraftPanelStateChanged();
        LogWindowEvent("AddButton_Click.Done");
    }

    private async void ToggleDoneButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ClearPendingDelete();
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("ToggleDoneButton_Click.InvalidTag");
            return;
        }

        if (CanToggleDoneFromCurrentView(item))
        {
            item.ActionOpen = true;
            item.Completing = true;
            LogWindowEvent("ToggleDoneButton_Click.Animate", $"ItemId={item.Id}");
            await Task.Delay(230);
        }

        ToggleDone(item);
        LogWindowEvent("ToggleDoneButton_Click.Done", $"ItemId={item.Id},CompletedAt={(item.CompletedAt is null ? "null" : item.CompletedAt.Value.ToString("O"))}");
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("PinButton_Click.InvalidTag");
            return;
        }

        TogglePinned(item);
        LogWindowEvent("PinButton_Click.Done", $"ItemId={item.Id},Pinned={item.Pinned}");
    }

    private void TodoItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CancelPendingActionToggle();

        if (IsDraftPanelActiveInCurrentView)
        {
            ClearPendingDelete();
            ClearTodoActions();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            LogWindowEvent("TodoItem_MouseLeftButtonDown.SkipDraftOpen");
            return;
        }

        if (e.OriginalSource is DependencyObject source && FindParent<Button>(source) is not null)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject textSource && FindParent<TextBox>(textSource) is not null)
        {
            return;
        }

        if (sender is not Border { Tag: TodoItem item })
        {
            return;
        }

        if (_allItems.Any(todo => todo.PendingDelete))
        {
            ClearPendingDelete();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            e.Handled = true;
            LogWindowEvent("TodoItem_MouseLeftButtonDown.ClearPendingDeleteOnly", $"ItemId={item.Id}");
            return;
        }

        if (e.ClickCount >= 2)
        {
            _deleteHoldTimer.Stop();
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            BeginTodoEdit(item);
            e.Handled = true;
            LogWindowEvent("TodoItem_MouseLeftButtonDown.BeginEdit", $"ItemId={item.Id}");
            return;
        }

        if (item.IsEditing)
        {
            ClearPendingDelete();
            _pressedTodoItem = item;
            _pendingDeleteItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            return;
        }

        if (item.ActionOpen || item.Completing)
        {
            ClearPendingDelete();
            _pressedTodoItem = item;
            _pendingDeleteItem = null;
            _deleteHoldTriggered = false;
            _deleteHoldTimer.Stop();
            LogWindowEvent("TodoItem_MouseLeftButtonDown.SkipDeleteHold", $"ItemId={item.Id},ActionOpen={item.ActionOpen},Completing={item.Completing}");
            return;
        }

        ClearPendingDelete();
        _pendingDeleteItem = item;
        _pressedTodoItem = item;
        _deleteHoldTriggered = false;
        _deleteHoldTimer.Stop();
        _deleteHoldTimer.Start();
    }

    private void TodoItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _deleteHoldTimer.Stop();
        if (IsDraftPanelActiveInCurrentView)
        {
            _pendingDeleteItem = null;
            _pressedTodoItem = null;
            _deleteHoldTriggered = false;
            ClearTodoActions();
            LogWindowEvent("TodoItem_MouseLeftButtonUp.SkipDraftOpen");
            return;
        }

        if (sender is Border { Tag: TodoItem item } && item == _pressedTodoItem && !_deleteHoldTriggered && !item.PendingDelete)
        {
            if (!item.IsEditing && CanToggleDoneFromCurrentView(item))
            {
                QueueActionToggle(item);
            }
            else
            {
                ClearTodoActions();
            }
        }

        _pendingDeleteItem = null;
        _pressedTodoItem = null;
        _deleteHoldTriggered = false;
    }

    private void TodoItem_MouseLeave(object sender, MouseEventArgs e)
    {
        _deleteHoldTimer.Stop();
        CancelPendingActionToggle();
        _pendingDeleteItem = null;
        _pressedTodoItem = null;
        _deleteHoldTriggered = false;
    }

    private void DeleteConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TodoItem item })
        {
            LogWindowEvent("DeleteConfirmButton_Click.InvalidTag");
            return;
        }

        DeleteTodo(item);
        LogWindowEvent("DeleteConfirmButton_Click.Done", $"ItemId={item.Id}");
    }

    private void CancelDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        LogWindowEvent("CancelDeleteButton_Click");
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingDelete();
        HideWindow();
        LogWindowEvent("HideButton_Click");
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        IsNightTheme = !IsNightTheme;
    }

    private void DraftCancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsDraftEditMode)
        {
            CancelTodoEdit(_editingTodoItem);
            return;
        }

        IsDraftOpen = false;
        LogWindowEvent("DraftCancelButton_Click.CloseDraft");
    }

    private void DraftTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (IsDraftEditMode)
            {
                CancelTodoEdit(_editingTodoItem);
            }
            else
            {
                IsDraftOpen = false;
                LogWindowEvent("DraftTextBox_PreviewKeyDown.CloseDraft");
            }
            return;
        }

        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        e.Handled = true;
        AddButton_Click(sender, new RoutedEventArgs());
        LogWindowEvent("DraftTextBox_PreviewKeyDown.Submit");
    }
}
