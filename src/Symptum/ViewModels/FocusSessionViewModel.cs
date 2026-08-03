using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Common.Helpers;
using Symptum.Helpers;

namespace Symptum.ViewModels;

public enum FocusSessionMode
{
    Focus,
    ShortBreak,
    LongBreak,
}

public partial class FocusSessionViewModel : ObservableObject
{
    public static FocusSessionViewModel Instance { get; } = new();

    public static string GetModeName(FocusSessionMode mode) => mode switch
    {
        FocusSessionMode.Focus => "Focus",
        FocusSessionMode.ShortBreak => "Short break",
        FocusSessionMode.LongBreak => "Long break",
        _ => string.Empty,
    };

    private readonly DispatcherTimer _ticker;
    private DateTime _endTime;
    private TimeSpan _remaining;
    private TimeSpan _total;
    private int _focusSessionsCompleted;

    public FocusSessionViewModel()
    {
        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _ticker.Tick += Ticker_Tick;

        FocusMinutes = AppDataHelper.GetValue(25.0, nameof(FocusMinutes));
        ShortBreakMinutes = AppDataHelper.GetValue(5.0, nameof(ShortBreakMinutes));
        LongBreakMinutes = AppDataHelper.GetValue(15.0, nameof(LongBreakMinutes));
        IntervalsPerSession = AppDataHelper.GetValue(4.0, nameof(IntervalsPerSession));
        NotificationsEnabled = AppDataHelper.GetValue(true, nameof(NotificationsEnabled));
        AutoStartNext = AppDataHelper.GetValue(true, nameof(AutoStartNext));

        _total = GetDuration(Mode);
        _remaining = _total;
        UpdateDisplay();
    }

    #region Properties

    [ObservableProperty]
    public partial FocusSessionMode Mode { get; set; }

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial string TimeText { get; set; } = "25:00";

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial double FocusMinutes { get; set; } = 25;

    [ObservableProperty]
    public partial double ShortBreakMinutes { get; set; } = 5;

    [ObservableProperty]
    public partial double LongBreakMinutes { get; set; } = 15;

    [ObservableProperty]
    public partial double IntervalsPerSession { get; set; } = 4;

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool AutoStartNext { get; set; } = true;

    #endregion

    partial void OnModeChanged(FocusSessionMode value) => ResetToMode(value);

    partial void OnFocusMinutesChanged(double value)
    {
        AppDataHelper.SetValue(value, nameof(FocusMinutes));
        UpdateDurationIfActive(FocusSessionMode.Focus, value);
    }

    partial void OnShortBreakMinutesChanged(double value)
    {
        AppDataHelper.SetValue(value, nameof(ShortBreakMinutes));
        UpdateDurationIfActive(FocusSessionMode.ShortBreak, value);
    }

    partial void OnLongBreakMinutesChanged(double value)
    {
        AppDataHelper.SetValue(value, nameof(LongBreakMinutes));
        UpdateDurationIfActive(FocusSessionMode.LongBreak, value);
    }

    partial void OnIntervalsPerSessionChanged(double value) => AppDataHelper.SetValue(value, nameof(IntervalsPerSession));

    partial void OnNotificationsEnabledChanged(bool value) => AppDataHelper.SetValue(value, nameof(NotificationsEnabled));

    partial void OnAutoStartNextChanged(bool value) => AppDataHelper.SetValue(value, nameof(AutoStartNext));

    private void Start()
    {
        if (IsRunning) return;
        if (_remaining <= TimeSpan.Zero) _remaining = _total;

        _endTime = DateTime.Now + _remaining;
        _ticker.Start();
        IsRunning = true;
        UpdateDisplay();
    }

    private void Pause()
    {
        if (!IsRunning) return;

        _ticker.Stop();
        _remaining = _endTime - DateTime.Now;
        if (_remaining < TimeSpan.Zero) _remaining = TimeSpan.Zero;
        IsRunning = false;
        UpdateDisplay();
    }

    [RelayCommand]
    private void Toggle()
    {
        if (IsRunning) Pause();
        else Start();
    }

    [RelayCommand]
    private void Reset()
    {
        _ticker.Stop();
        IsRunning = false;
        _remaining = _total;
        UpdateDisplay();
    }

    [RelayCommand]
    private void Skip()
    {
        _ticker.Stop();
        IsRunning = false;
        UpdateDisplay();
        OnSessionEnded();
    }

    private void Ticker_Tick(object? sender, object e)
    {
        _remaining = _endTime - DateTime.Now;
        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            _ticker.Stop();
            IsRunning = false;
            UpdateDisplay();
            OnSessionEnded();
            return;
        }
        UpdateDisplay();
    }

    private void OnSessionEnded()
    {
        FocusSessionMode ended = Mode;
        FocusSessionMode next = GetNextMode(ended);
        Mode = next;

#if WINDOWS
        string title;
        string message;
        if (ended == FocusSessionMode.Focus)
        {
            title = "Focus session completed";
            message = AutoStartNext ? $"{GetModeName(next)} started." : $"Time for a {GetModeName(next).ToLowerInvariant()}.";
        }
        else
        {
            title = $"{GetModeName(ended)} over";
            message = AutoStartNext ? $"Focus session started." : "Ready to focus?";
        }

        NotificationHelper.Show(title, message);
#endif

        if (AutoStartNext)
        {
            Start();
        }
    }

    private FocusSessionMode GetNextMode(FocusSessionMode ended) => ended switch
    {
        FocusSessionMode.Focus => ++_focusSessionsCompleted % IntervalsCount == 0 ? FocusSessionMode.LongBreak : FocusSessionMode.ShortBreak,
        _ => FocusSessionMode.Focus,
    };

    private int IntervalsCount => Math.Max(1, (int)Math.Round(IntervalsPerSession));

    private void ResetToMode(FocusSessionMode mode)
    {
        _ticker.Stop();
        _total = GetDuration(mode);
        _remaining = _total;
        IsRunning = false;
        UpdateDisplay();
    }

    private void UpdateDurationIfActive(FocusSessionMode mode, double minutes)
    {
        if (Mode == mode && !IsRunning)
        {
            _total = TimeSpan.FromMinutes(Math.Max(1, minutes));
            _remaining = _total;
            UpdateDisplay();
        }
    }

    private TimeSpan GetDuration(FocusSessionMode mode) => mode switch
    {
        FocusSessionMode.Focus => TimeSpan.FromMinutes(Math.Max(1, FocusMinutes)),
        FocusSessionMode.ShortBreak => TimeSpan.FromMinutes(Math.Max(1, ShortBreakMinutes)),
        FocusSessionMode.LongBreak => TimeSpan.FromMinutes(Math.Max(1, LongBreakMinutes)),
        _ => TimeSpan.Zero,
    };

    private void UpdateDisplay()
    {
        TimeText = $"{Math.Floor(_remaining.TotalMinutes):00}:{_remaining.Seconds:00}";
        Progress = _total.TotalSeconds > 0
            ? Math.Min(0.9999, 1 - _remaining.TotalSeconds / _total.TotalSeconds)
            : 0;
    }
}
