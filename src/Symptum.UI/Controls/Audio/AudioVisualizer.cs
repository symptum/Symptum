using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Symptum.Core.Helpers;
using Symptum.UI.Controls.Audio;
using Windows.Foundation;
using XamlPath = Microsoft.UI.Xaml.Shapes.Path;

namespace Symptum.UI.Controls;

public partial class AudioVisualizer : Control
{
    private bool _disposed = false;
    private Canvas? _waveCanvas;
    private XamlPath? _wavePath;
    private XamlPath? _waveProgressPath;
    private RectangleGeometry? _progressClip;
    private Rectangle? _playheadIndicator;
    private Rectangle? _hoverIndicator;
    private UIElement? _seekLayer;
    private Button? _actionButton;
    private FontIcon? _actionIcon;
    private Button? _playPauseButton;
    private Button? _jumpToStartButton;
    private ComboBox? _rateComboBox;
    private bool _updatingRate;
    private ToggleButton? _repeatButton;
    private FontIcon? _playPauseIcon;
    private Run? _positionText;
    private Run? _durationText;
    private ProgressRing? _loadingRing;
    private StackPanel? _transportPanel;

    private readonly List<float> _samples = [];
    private readonly List<Point> _wavePoints = [];
    private bool _mediaEnded;
    private bool _isSeekingFromPointer;
    private bool _hasSource;
    private int _loadVersion;
    private CancellationTokenSource? _loadCancellation;

    public AudioVisualizer()
    {
        DefaultStyleKey = typeof(AudioVisualizer);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public event EventHandler? ActionButtonClicked;
    public event EventHandler? MediaOpened;

    public event EventHandler? MediaEnded;

    public event EventHandler<string>? MediaFailed;

    #region Lifecycle

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachTemplateHandlers();

        _waveCanvas = GetTemplateChild("PART_WaveCanvas") as Canvas;
        _wavePath = GetTemplateChild("PART_WavePath") as XamlPath;
        _waveProgressPath = GetTemplateChild("PART_WaveProgressPath") as XamlPath;
        _playheadIndicator = GetTemplateChild("PART_PlayheadIndicator") as Rectangle;
        _hoverIndicator = GetTemplateChild("PART_HoverIndicator") as Rectangle;
        _seekLayer = GetTemplateChild("PART_SeekLayer") as UIElement;
        _actionButton = GetTemplateChild("PART_ActionButton") as Button;
        _actionIcon = GetTemplateChild("PART_ActionButtonIcon") as FontIcon;
        _playPauseButton = GetTemplateChild("PART_PlayPauseButton") as Button;
        _jumpToStartButton = GetTemplateChild("PART_JumpToStartButton") as Button;
        _rateComboBox = GetTemplateChild("PART_RateComboBox") as ComboBox;
        _repeatButton = GetTemplateChild("PART_RepeatButton") as ToggleButton;
        _playPauseIcon = GetTemplateChild("PART_PlayPauseIcon") as FontIcon;
        _positionText = GetTemplateChild("PART_PositionText") as Run;
        _durationText = GetTemplateChild("PART_DurationText") as Run;
        _loadingRing = GetTemplateChild("PART_LoadingRing") as ProgressRing;
        _transportPanel = GetTemplateChild("PART_TransportPanel") as StackPanel;

        _transportPanel?.Visibility = ShowTransportControls ? Visibility.Visible : Visibility.Collapsed;
        _waveCanvas?.SizeChanged += OnWaveCanvasSizeChanged;

        if (_seekLayer != null)
        {
            _seekLayer.PointerPressed += OnSeekLayerPointerPressed;
            _seekLayer.PointerMoved += OnSeekLayerPointerMoved;
            _seekLayer.PointerReleased += OnSeekLayerPointerReleased;
            _seekLayer.PointerExited += OnSeekLayerPointerExited;
            _seekLayer.PointerCaptureLost += OnSeekLayerPointerCaptureLost;
        }

        _actionButton?.Click += OnActionButton_Click;
        _actionIcon?.Glyph = ActionButtonGlyph;
        _playPauseButton?.Click += OnPlayPauseButtonClick;
        _jumpToStartButton?.Click += OnJumpToStartButtonClick;
        _repeatButton?.Click += OnRepeatButtonClick;
        _rateComboBox?.SelectionChanged += OnRateComboBoxSelectionChanged;
        UpdatePlayPauseButton(IsPlaying);
        UpdateRateComboBox();
        RefreshFromState();
    }

    private void DetachTemplateHandlers()
    {
        _waveCanvas?.SizeChanged -= OnWaveCanvasSizeChanged;

        if (_seekLayer != null)
        {
            _seekLayer.PointerPressed -= OnSeekLayerPointerPressed;
            _seekLayer.PointerMoved -= OnSeekLayerPointerMoved;
            _seekLayer.PointerReleased -= OnSeekLayerPointerReleased;
            _seekLayer.PointerExited -= OnSeekLayerPointerExited;
            _seekLayer.PointerCaptureLost -= OnSeekLayerPointerCaptureLost;
        }

        _actionButton?.Click -= OnActionButton_Click;
        _playPauseButton?.Click -= OnPlayPauseButtonClick;
        _jumpToStartButton?.Click -= OnJumpToStartButtonClick;
        _repeatButton?.Click -= OnRepeatButtonClick;
        _rateComboBox?.SelectionChanged -= OnRateComboBoxSelectionChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeSessionEvents();
        RenderWave();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeSessionEvents();
        if (IsPlaying && !_disposed)
        {
            Pause();
        }
    }

    private void RefreshFromState()
    {
        _positionText?.Text = FormatTime(Position);
        _durationText?.Text = FormatTime(Duration);
        UpdateProgress();
        MovePlayhead();
        RenderWave();
    }

    private void OnActionButton_Click(object sender, RoutedEventArgs e) => ActionButtonClicked?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Loading

    public void Unload()
    {
        if (_disposed) return;
        _disposed = true;
        Source = null;
        DetachTemplateHandlers();
        _mediaPlayer?.Dispose();
    }

    private async Task LoadAudioAsync(StorageFile file)
    {
        _loadCancellation?.Cancel();

        if (file.FileType != FileHelper.Mp3FileExtension) return;

        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        var version = ++_loadVersion;

        _hasSource = true;
        _mediaEnded = false;
        SetSource(file);

        IsLoading = true;
        try
        {
            var (samples, duration) = await Task.Run(
                async () =>
                {
                    using var stream = await file.OpenStreamForReadAsync();
                    var samples = AudioSampleDecoder.DecodeToSamples(stream, out var duration, token);
                    return (samples, duration);
                },
                CancellationToken.None);

            if (version != _loadVersion || token.IsCancellationRequested)
            {
                return;
            }

            _samples.Clear();
            _samples.AddRange(samples);

            Duration = TimeSpan.FromSeconds(duration);
            Position = TimeSpan.Zero;

            RenderWave();
            MediaOpened?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        { }
        catch (Exception ex) when (version == _loadVersion)
        {
            MediaFailed?.Invoke(this, $"{file.Name}: {ex.Message}");
        }
        finally
        {
            if (version == _loadVersion)
            {
                _loadCancellation?.Dispose();
                _loadCancellation = null;
                IsLoading = false;
            }
        }
    }

    private void ClearAudio()
    {
        _loadCancellation?.Cancel();
        _loadVersion++;
        _hasSource = false;
        _mediaEnded = false;
        _samples.Clear();
        _wavePoints.Clear();
        Duration = TimeSpan.Zero;
        Position = TimeSpan.Zero;
        StopPlaybackCore();
        RenderWave();
    }

    #endregion

    #region Transport Controls

    public void Play()
    {
        if (!_hasSource || Duration <= TimeSpan.Zero)
        {
            return;
        }

        if (!IsPlaying)
        {
            if (_mediaEnded)
            {
                Seek(TimeSpan.Zero);
            }

            PlayCore();
            IsPlaying = true;
        }
    }

    public void Pause()
    {
        if (IsPlaying)
        {
            PauseCore();
            IsPlaying = false;
        }
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public void Seek(TimeSpan position)
    {
        if (Duration > TimeSpan.Zero)
        {
            var seconds = Math.Clamp(position.TotalSeconds, 0d, Duration.TotalSeconds);
            position = TimeSpan.FromSeconds(seconds);
        }

        _mediaEnded = position >= Duration && Duration > TimeSpan.Zero;
        Position = position;
        SeekCore(position.TotalSeconds);
    }

    private void UpdatePlayPauseButton(bool isPlaying = false)
    {
        ToolTipService.SetToolTip(_playPauseButton, isPlaying ? "Pause" : "Play");
        _playPauseIcon?.Glyph = isPlaying ? CommonGlyphs.Pause : CommonGlyphs.Play;
    }

    private void OnPlayPauseButtonClick(object sender, RoutedEventArgs e) => TogglePlayPause();

    public void JumpToStart() => Seek(TimeSpan.Zero);

    public void SetPlaybackRate(double rate)
    {
        if (rate > 0)
        {
            PlaybackRate = rate;
        }
    }

    private void UpdateRateComboBox()
    {
        if (_rateComboBox == null)
        {
            return;
        }

        _updatingRate = true;
        try
        {
            foreach (var item in _rateComboBox.Items)
            {
                if (item is ComboBoxItem comboItem &&
                    double.TryParse(comboItem.Tag?.ToString(), out var rate) &&
                    Math.Abs(rate - PlaybackRate) < 0.001)
                {
                    _rateComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _updatingRate = false;
        }
    }

    private void OnRateComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingRate || _rateComboBox?.SelectedItem is not ComboBoxItem item ||
            !double.TryParse(item.Tag?.ToString(), out var rate))
        {
            return;
        }

        SetPlaybackRate(rate);
    }

    private void OnJumpToStartButtonClick(object sender, RoutedEventArgs e) => JumpToStart();

    private void OnRepeatButtonClick(object sender, RoutedEventArgs e)
    {
        if (_repeatButton != null)
        {
            IsRepeatEnabled = _repeatButton.IsChecked == true;
        }
    }

    #endregion

    #region Waveform

    public static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? $"{time:hh\\:mm\\:ss}"
            : $"{time:mm\\:ss}";
    }

    public IReadOnlyList<Point> GetPoints(double controlWidth, double controlHeight, double itemWidth = 2, double spacing = 1)
    {
        if (_samples.Count == 0 || controlWidth <= 0 || controlHeight <= 0)
        {
            return [];
        }

        var numOfItems = (int)(controlWidth / (itemWidth + spacing));
        if (numOfItems <= 0)
        {
            return [];
        }

        var numOfSamplesPerItem = Math.Max(_samples.Count / numOfItems, 1);
        var points = new List<Point>(numOfItems);

        for (var i = 0; i < numOfItems; i++)
        {
            float max = 0;
            var start = i * numOfSamplesPerItem;
            var end = Math.Min(start + numOfSamplesPerItem, _samples.Count);

            for (var j = start; j < end; j++)
            {
                var sample = Math.Abs(_samples[j]);
                if (sample > max)
                {
                    max = sample;
                }
            }

            // If the width is too small, the wave will be stretched vertically.
            // And will defeat the purpose of the waveform.
            // That is to easily recognize and memorize audio waves.
            var normalizedHeight = Math.Min(controlHeight, controlWidth);
            var height = Math.Clamp(max, 0f, 1f) * normalizedHeight / 2;
            points.Add(new Point(i * (itemWidth + spacing), (controlHeight / 2) - height));
        }

        return points;
    }

    private void RenderWave()
    {
        if (_waveCanvas == null || _wavePath == null || _waveProgressPath == null)
        {
            return;
        }

        var width = _waveCanvas.ActualWidth;
        var height = _waveCanvas.ActualHeight;
        if (width < 4 || height < 4)
        {
            return;
        }

        var points = GetPoints(width, height, WaveItemWidth, WaveItemSpacing);
        _wavePoints.Clear();
        _wavePoints.AddRange(points);

        _wavePath.Data = BuildGeometry(_wavePoints.Count);
        _waveProgressPath.Data = BuildGeometry(_wavePoints.Count);
        if (_progressClip == null)
        {
            _progressClip = new RectangleGeometry();
            _waveProgressPath.Clip = _progressClip;
        }

        UpdateProgress();
    }

    private GeometryGroup BuildGeometry(int barCount)
    {
        var group = new GeometryGroup
        {
            FillRule = FillRule.Nonzero,
        };

        if (barCount <= 0 || _wavePoints.Count == 0)
        {
            return group;
        }

        var halfHeight = _waveCanvas?.ActualHeight / 2 ?? 0;

        var last = Math.Min(barCount, _wavePoints.Count);
        for (var i = 0; i < last; i++)
        {
            var point = _wavePoints[i];
            var barHeight = Math.Max((halfHeight - point.Y) * 2, 1);
            group.Children.Add(new RectangleGeometry
            {
                Rect = new Rect(point.X, point.Y, WaveItemWidth, barHeight),
            });
        }

        return group;
    }

    private void UpdateProgress()
    {
        if (_waveProgressPath == null || _progressClip == null || _waveCanvas == null)
        {
            return;
        }

        var width = _waveCanvas.ActualWidth;
        var height = _waveCanvas.ActualHeight;
        if (width < 4 || height < 4 || Duration <= TimeSpan.Zero)
        {
            _progressClip.Rect = new Rect(0, 0, 0, height);
            return;
        }

        var highlight = Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds, 0d, 1d);
        var span = WaveItemWidth + WaveItemSpacing;
        var barWidth = Math.Floor(highlight * width / span) * span;
        _progressClip.Rect = new Rect(0, 0, barWidth, height);
    }

    private void UpdatePosition()
    {
        _positionText?.Text = FormatTime(Position);

        UpdateProgress();
        MovePlayhead();
    }

    private void UpdateDuration()
    {
        _durationText?.Text = FormatTime(Duration);

        UpdateProgress();
        MovePlayhead();
    }

    private void MovePlayhead()
    {
        if (_playheadIndicator == null || _waveCanvas == null)
        {
            return;
        }

        var width = _waveCanvas.ActualWidth > 0 ? _waveCanvas.ActualWidth : ActualWidth;
        if (width < 4 || Duration <= TimeSpan.Zero)
        {
            _playheadIndicator.Visibility = Visibility.Collapsed;
            return;
        }

        _playheadIndicator.Visibility = Visibility.Visible;
        var playheadWidth = double.IsNaN(_playheadIndicator.Width) ? 2 : _playheadIndicator.Width;
        var x = Math.Clamp((Position.TotalSeconds / Duration.TotalSeconds * width) - (playheadWidth / 2), 0, Math.Max(width - playheadWidth, 0));
        _playheadIndicator.Margin = new Thickness(x, 0, 0, 0);
    }

    private void OnWaveCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderWave();
        MovePlayhead();
    }

    #endregion

    #region Pointer Interaction

    private void OnSeekLayerPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_hasSource || Duration <= TimeSpan.Zero)
        {
            return;
        }

        _isSeekingFromPointer = true;
        var element = (UIElement)sender;
        element.CapturePointer(e.Pointer);
        HideHoverIndicator();
        UpdatePositionFromPointer(e);
        e.Handled = true;
    }

    private void OnSeekLayerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isSeekingFromPointer)
        {
            UpdatePositionFromPointer(e);
            e.Handled = true;
        }
        else
        {
            UpdateHoverIndicator(e);
        }
    }

    private void OnSeekLayerPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isSeekingFromPointer)
        {
            _isSeekingFromPointer = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
            UpdatePositionFromPointer(e);
        }

        HideHoverIndicator();
    }

    private void OnSeekLayerPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSeekingFromPointer)
        {
            HideHoverIndicator();
        }
    }

    private void OnSeekLayerPointerCaptureLost(object sender, PointerRoutedEventArgs e) => _isSeekingFromPointer = false;

    private void UpdatePositionFromPointer(PointerRoutedEventArgs e)
    {
        var width = _waveCanvas?.ActualWidth ?? ActualWidth;
        if (width <= 0 || Duration <= TimeSpan.Zero)
        {
            return;
        }

        var x = Math.Clamp(e.GetCurrentPoint(this).Position.X, 0, width);
        Seek(TimeSpan.FromSeconds(x / width * Duration.TotalSeconds));
    }

    private void UpdateHoverIndicator(PointerRoutedEventArgs e)
    {
        if (_hoverIndicator == null || Duration <= TimeSpan.Zero)
        {
            return;
        }

        var width = _waveCanvas?.ActualWidth ?? ActualWidth;
        if (width <= 0)
        {
            return;
        }

        var x = Math.Clamp(e.GetCurrentPoint(this).Position.X, 0, width);
        var hoverWidth = double.IsNaN(_hoverIndicator.Width) ? 4 : _hoverIndicator.Width;
        _hoverIndicator.Visibility = Visibility.Visible;
        _hoverIndicator.Margin = new Thickness(x - (hoverWidth / 2), 0, 0, 0);
    }

    private void HideHoverIndicator()
    {
        _hoverIndicator?.Visibility = Visibility.Collapsed;
    }

    #endregion
}
