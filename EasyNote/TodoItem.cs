using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EasyNote;

public class TodoItem : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _text = string.Empty;
    private bool _pinned;
    private DateTime _createdAt;
    private DateTime? _completedAt;
    private bool _pendingDelete;
    private bool _actionOpen;
    private bool _completing;
    private bool _suppressActionOpenAnimation;
    private bool _isNew;
    private bool _isEditing;
    private string _editingText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set
        {
            if (SetField(ref _text, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
    }

    public bool Pinned
    {
        get => _pinned;
        set
        {
            if (SetField(ref _pinned, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
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
    public bool ActionOpen
    {
        get => _actionOpen;
        set
        {
            if (SetField(ref _actionOpen, value))
            {
                OnPropertyChanged(nameof(ActionSurfaceOffset));
                OnPropertyChanged(nameof(ActionButtonOpacity));
                OnPropertyChanged(nameof(ActionButtonScale));
            }
        }
    }

    [JsonIgnore]
    public bool Completing
    {
        get => _completing;
        set => SetField(ref _completing, value);
    }

    [JsonIgnore]
    public bool SuppressActionOpenAnimation
    {
        get => _suppressActionOpenAnimation;
        set => SetField(ref _suppressActionOpenAnimation, value);
    }

    [JsonIgnore]
    public bool IsNew
    {
        get => _isNew;
        set => SetField(ref _isNew, value);
    }

    [JsonIgnore]
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetField(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
    }

    [JsonIgnore]
    public string EditingText
    {
        get => _editingText;
        set
        {
            if (SetField(ref _editingText, value))
            {
                OnPropertyChanged(nameof(SecondaryText));
            }
        }
    }

    [JsonIgnore]
    public string SecondaryText =>
        IsEditing
            ? string.Empty
            : CompletedAt is null
                ? (Pinned ? FormatPinnedCreatedText(CreatedAt) : FormatCreatedText(CreatedAt))
                : FormatCompletedText(CompletedAt.Value);

    [JsonIgnore]
    public double ActionSurfaceOffset => ActionOpen ? 50 : 0;

    [JsonIgnore]
    public double ActionButtonOpacity => ActionOpen ? 1 : 0;

    [JsonIgnore]
    public double ActionButtonScale => ActionOpen ? 1 : 0.72;

    private static string FormatCreatedText(DateTime date)
    {
        var culture = CultureInfo.CurrentUICulture;
        return culture.TwoLetterISOLanguageName switch
        {
            "zh" => $"创建于 {FormatMonthDay(date, culture)}",
            "ja" => $"作成 {FormatMonthDay(date, culture)}",
            "ko" => $"생성 {FormatMonthDay(date, culture)}",
            _ => $"Created on {FormatMonthDay(date, culture)}"
        };
    }

    private static string FormatCompletedText(DateTime date)
    {
        var culture = CultureInfo.CurrentUICulture;
        return culture.TwoLetterISOLanguageName switch
        {
            "zh" => $"完成于 {FormatMonthDay(date, culture)}",
            "ja" => $"完了 {FormatMonthDay(date, culture)}",
            "ko" => $"완료 {FormatMonthDay(date, culture)}",
            _ => $"Completed on {FormatMonthDay(date, culture)}"
        };
    }

    private static string FormatPinnedCreatedText(DateTime date)
    {
        var culture = CultureInfo.CurrentUICulture;
        return culture.TwoLetterISOLanguageName switch
        {
            "zh" => $"已置顶 • 创建于 {FormatMonthDay(date, culture)}",
            "ja" => $"固定済み • 作成 {FormatMonthDay(date, culture)}",
            "ko" => $"고정됨 • 생성 {FormatMonthDay(date, culture)}",
            _ => $"Pinned • Created on {FormatMonthDay(date, culture)}"
        };
    }

    private static string FormatMonthDay(DateTime date, CultureInfo culture)
        => date.ToString(culture.DateTimeFormat.MonthDayPattern, culture);

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
