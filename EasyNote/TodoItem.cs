using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EasyNote;

public class TodoItem : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _text = string.Empty;
    private DateTime _noteDate;
    private bool _pinned;
    private DateTime _createdAt;
    private DateTime? _completedAt;
    private bool _pendingDelete;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public DateTime NoteDate
    {
        get => _noteDate;
        set => SetField(ref _noteDate, value);
    }

    public bool Pinned
    {
        get => _pinned;
        set => SetField(ref _pinned, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (SetField(ref _createdAt, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (SetField(ref _completedAt, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
    }

    [JsonIgnore]
    public bool PendingDelete
    {
        get => _pendingDelete;
        set => SetField(ref _pendingDelete, value);
    }

    [JsonIgnore]
    public string SecondaryText =>
        CompletedAt is null
            ? $"创建日期 {CreatedAt:yyyy/MM/dd HH:mm}"
            : $"完成日期 {CompletedAt.Value:yyyy/MM/dd HH:mm}";

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
