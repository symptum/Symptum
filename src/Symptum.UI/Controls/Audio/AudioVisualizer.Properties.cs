namespace Symptum.UI.Controls;

public partial class AudioVisualizer
{
    #region Source

    public static readonly DependencyProperty SourceProperty =
    DependencyProperty.Register(
        nameof(Source),
        typeof(StorageFile),
        typeof(AudioVisualizer),
        new PropertyMetadata(null, OnSourceChanged));

    public StorageFile? Source
    {
        get => (StorageFile?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            if (e.NewValue is StorageFile file)
            {
                _ = visualizer.LoadAudioAsync(file);
            }
            else
            {
                visualizer.ClearAudio();
            }
        }
    }

    #endregion

    #region ShowTransportControls

    public static readonly DependencyProperty ShowTransportControlsProperty =
        DependencyProperty.Register(
            nameof(ShowTransportControls),
            typeof(bool),
            typeof(AudioVisualizer),
            new PropertyMetadata(true, OnShowTransportControlsChanged));

    public bool ShowTransportControls
    {
        get => (bool)GetValue(ShowTransportControlsProperty);
        set => SetValue(ShowTransportControlsProperty, value);
    }

    private static void OnShowTransportControlsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer._transportPanel?.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }


    #endregion

    #region Position

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register(
            nameof(Position),
            typeof(TimeSpan),
            typeof(AudioVisualizer),
            new PropertyMetadata(TimeSpan.Zero, OnPositionChanged));

    public TimeSpan Position
    {
        get => (TimeSpan)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer.UpdatePosition();
        }
    }

    #endregion

    #region Duration

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(AudioVisualizer),
            new PropertyMetadata(TimeSpan.Zero, OnDurationChanged));

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        private set => SetValue(DurationProperty, value);
    }

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer.UpdateDuration();
        }
    }

    #endregion

    #region IsPlaying

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(AudioVisualizer),
            new PropertyMetadata(false, OnIsPlayingChanged));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
            visualizer.UpdatePlayPauseButton((bool)e.NewValue);
    }

    #endregion

    #region IsLoading

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(
            nameof(IsLoading),
            typeof(bool),
            typeof(AudioVisualizer),
            new PropertyMetadata(false, OnIsLoadingChanged));


    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer._loadingRing?.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    #endregion

    #region WaveItemWidth

    public static readonly DependencyProperty WaveItemWidthProperty =
        DependencyProperty.Register(
            nameof(WaveItemWidth),
            typeof(double),
            typeof(AudioVisualizer),
            new PropertyMetadata(2d, OnWaveOptionChanged));

    public double WaveItemWidth
    {
        get => (double)GetValue(WaveItemWidthProperty);
        set => SetValue(WaveItemWidthProperty, value);
    }

    private static void OnWaveOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer.RenderWave();
        }
    }

    #endregion

    #region WaveItemSpacing

    public static readonly DependencyProperty WaveItemSpacingProperty =
        DependencyProperty.Register(
            nameof(WaveItemSpacing),
            typeof(double),
            typeof(AudioVisualizer),
            new PropertyMetadata(1d, OnWaveOptionChanged));

    public double WaveItemSpacing
    {
        get => (double)GetValue(WaveItemSpacingProperty);
        set => SetValue(WaveItemSpacingProperty, value);
    }

    #endregion

    #region WaveBrush

    public static readonly DependencyProperty WaveBrushProperty =
        DependencyProperty.Register(
            nameof(WaveBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(null));

    public Brush? WaveBrush
    {
        get => (Brush?)GetValue(WaveBrushProperty);
        set => SetValue(WaveBrushProperty, value);
    }

    #endregion

    #region WaveProgressBrush

    public static readonly DependencyProperty WaveProgressBrushProperty =
        DependencyProperty.Register(
            nameof(WaveProgressBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(null));

    public Brush? WaveProgressBrush
    {
        get => (Brush?)GetValue(WaveProgressBrushProperty);
        set => SetValue(WaveProgressBrushProperty, value);
    }

    #endregion

    #region HoverBrush

    public static readonly DependencyProperty HoverBrushProperty =
        DependencyProperty.Register(
            nameof(HoverBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(null));

    public Brush? HoverBrush
    {
        get => (Brush?)GetValue(HoverBrushProperty);
        set => SetValue(HoverBrushProperty, value);
    }

    #endregion

    #region PlaybackRate

    public static readonly DependencyProperty PlaybackRateProperty =
        DependencyProperty.Register(
            nameof(PlaybackRate),
            typeof(double),
            typeof(AudioVisualizer),
            new PropertyMetadata(1d, OnPlaybackRateChanged));

    public double PlaybackRate
    {
        get => (double)GetValue(PlaybackRateProperty);
        set => SetValue(PlaybackRateProperty, value);
    }

    private static void OnPlaybackRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer.UpdateRateComboBox();
            visualizer.ApplyPlaybackRateCore(visualizer.PlaybackRate);
        }
    }

    #endregion

    #region IsRepeatEnabled

    public static readonly DependencyProperty IsRepeatEnabledProperty =
        DependencyProperty.Register(
            nameof(IsRepeatEnabled),
            typeof(bool),
            typeof(AudioVisualizer),
            new PropertyMetadata(false));

    public bool IsRepeatEnabled
    {
        get => (bool)GetValue(IsRepeatEnabledProperty);
        set => SetValue(IsRepeatEnabledProperty, value);
    }

    #endregion

    #region ActionButtonGlyph

    public static readonly DependencyProperty ActionButtonGlyphProperty = DependencyProperty.Register(
        nameof(ActionButtonGlyph),
        typeof(string),
        typeof(AudioVisualizer),
        new PropertyMetadata(null, OnActionButtonGlyphChanged));

    private static void OnActionButtonGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is AudioVisualizer visualizer && args.NewValue is string glyph)
        {
            visualizer._actionIcon?.Glyph = glyph;
        }
    }

    public string ActionButtonGlyph
    {
        get => (string)GetValue(ActionButtonGlyphProperty);
        set => SetValue(ActionButtonGlyphProperty, value);
    }

    #endregion
}
