using Symptum.UI;
using Symptum.ViewModels;
using Windows.Foundation;

namespace Symptum.Pages;

public sealed partial class FocusSessionPage : NavigablePage
{

    private readonly Brush? focusVisualBrush;
    private readonly Brush? shortBreakVisualBrush;
    private readonly Brush? longBreakVisualBrush;

    private double ringThickness = 0;

    public FocusSessionPage()
    {
        InitializeComponent();
        focusVisualBrush = Resources["FocusVisualBrush"] as Brush;
        shortBreakVisualBrush = Resources["ShortBreakVisualBrush"] as Brush;
        longBreakVisualBrush = Resources["LongBreakVisualBrush"] as Brush;
        Loaded += FocusSessionPage_Loaded;
        Unloaded += FocusSessionPage_Unloaded;
    }

    public FocusSessionViewModel ViewModel => FocusSessionViewModel.Instance;

    private void FocusSessionPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RebuildClock();
        UpdateMode();
        UpdateAnimation();
        UpdatePlayIcon();

        clockGrid.SizeChanged += ClockGrid_SizeChanged;
    }

    private void FocusSessionPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        clockGrid.SizeChanged -= ClockGrid_SizeChanged;
        scaleAnimation.Stop();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FocusSessionViewModel.IsRunning):
                UpdateAnimation();
                UpdatePlayIcon();
                break;

            case nameof(FocusSessionViewModel.AnimationsEnabled):
                UpdateAnimation();
                break;

            case nameof(FocusSessionViewModel.Progress):
                UpdateProgressArc();
                break;

            case nameof(FocusSessionViewModel.Mode):
                UpdateMode();
                UpdatePlayIcon();
                break;
        }
    }

    private void ModeSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (modeSegmented.SelectedIndex >= 0 &&
            Enum.IsDefined(typeof(FocusSessionMode), modeSegmented.SelectedIndex))
        {
            ViewModel.Mode = (FocusSessionMode)modeSegmented.SelectedIndex;
        }
    }

    private void UpdateMode()
    {
        modeSegmented.SelectedIndex = (int)ViewModel.Mode;
        var brush = ViewModel.Mode switch
        {
            FocusSessionMode.Focus => FocusSessionViewModel.FocusBrush,
            FocusSessionMode.ShortBreak => FocusSessionViewModel.ShortBreakBrush,
            FocusSessionMode.LongBreak => FocusSessionViewModel.LongBreakBrush,
            _ => null,
        };

        var VisualBrush = ViewModel.Mode switch
        {
            FocusSessionMode.Focus => focusVisualBrush,
            FocusSessionMode.ShortBreak => shortBreakVisualBrush,
            FocusSessionMode.LongBreak => longBreakVisualBrush,
            _ => null,
        };

        ringProgress.Stroke = brush;
        visual.Fill = VisualBrush;
        visual2.Fill = VisualBrush;
    }

    private void UpdateAnimation()
    {
        opacityAnimation.Stop();
        visual.Opacity = 0;
        visual2.Opacity = 0;
        scaleAnimation.Stop();
        if (ViewModel.AnimationsEnabled && ViewModel.IsRunning)
        {
            scaleAnimation.Begin();
            opacityAnimation.Begin();
        }
    }

    private void UpdatePlayIcon()
    {
        playIcon.Glyph = ViewModel.IsRunning ? CommonGlyphs.Pause : CommonGlyphs.Play;
    }

    private void RebuildClock()
    {
        double size = GetClockSize();
        if (size <= 0) return;

        ringTrack.Width = size;
        ringTrack.Height = size;
        ringProgress.Width = size;
        ringProgress.Height = size;

        visual2.Width = size;
        visual2.Height = size;

        UpdateProgressArc();
    }

    private void UpdateProgressArc()
    {
        double size = GetClockSize();
        if (size <= 0) return;

        double thickness = ringThickness;
        if (size < 160)
        {
            thickness = ringThickness * (size / 160);
        }
        ringTrack.StrokeThickness = thickness;
        ringProgress.StrokeThickness = thickness;

        double radius = size / 2 - thickness / 2;
        ringProgress.Data = BuildArc(size / 2, size / 2, radius, ViewModel.Progress);
    }

    private double GetClockSize()
    {
        double size = Math.Min(clockGrid.ActualWidth, clockGrid.ActualHeight);
        if (size <= 0) size = Math.Min(clockGrid.Width, clockGrid.Height);
        return size;
    }

    private static Geometry BuildArc(double cx, double cy, double radius, double progress)
    {
        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(cx, cy - radius),
            IsFilled = false,
            IsClosed = false,
        };
        geometry.Figures.Add(figure);

        if (progress > 0 && radius > 0)
        {
            double angle = (360 * progress - 90) * Math.PI / 180;
            var end = new Point(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = progress > 0.5,
                SweepDirection = SweepDirection.Clockwise,
            });
        }

        return geometry;
    }

    private void ClockGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RebuildClock();
    }

    protected override void OnLayoutChanged(int widthLevel, int heightLevel)
    {
        if (widthLevel == 2 && heightLevel == 2)
        {
            modeSegmented.Visibility = Visibility.Visible;
            clockGrid.MaxWidth = 480;
            clockGrid.MaxHeight = 480;
            timeTB.FontSize = 64;
            content.RowSpacing = 28;
            playButton.Width = 72;
            playButton.Height = 72;
            playButton.Padding = new(12);
            playButton.CornerRadius = new(36);
            playIcon.FontSize = 26;
            content.Margin = ContentMargin;
            visual.Width = 1000;
            visual.Height = 1000;
            visual.Margin = new(0, 0, 0, -600);
            ringThickness = 24;
        }
        else if (widthLevel >= 1 && heightLevel >= 1)
        {
            modeSegmented.Visibility = Visibility.Visible;
            clockGrid.MaxWidth = 260;
            clockGrid.MaxHeight = 260;
            timeTB.FontSize = 24;
            content.RowSpacing = 16;
            playButton.Width = 48;
            playButton.Height = 48;
            playButton.Padding = new(8);
            playButton.CornerRadius = new(24);
            playIcon.FontSize = 20;
            content.Margin = NarrowContentMargin;
            visual.Width = 600;
            visual.Height = 600;
            visual.Margin = new(0, 0, 0, -350);
            ringThickness = 14;
        }
        else
        {
            modeSegmented.Visibility = Visibility.Collapsed;
            clockGrid.MaxWidth = 180;
            clockGrid.MaxHeight = 180;
            timeTB.FontSize = 18;
            content.RowSpacing = 12;
            playButton.Width = 32;
            playButton.Height = 32;
            playButton.Padding = new(4);
            playButton.CornerRadius = new(16);
            playIcon.FontSize = 14;
            content.Margin = NarrowContentMargin;
            visual.Width = 200;
            visual.Height = 200;
            visual.Margin = new(0, 0, 0, -120);
            ringThickness = 6;
        }
    }
}
