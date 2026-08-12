using Symptum.ViewModels;
using Windows.Foundation;

namespace Symptum.Pages;

public sealed partial class FocusSessionPage : NavigablePage
{
    private const double RingThickness = 14;

    private readonly SolidColorBrush? focusBrush;
    private readonly SolidColorBrush? shortBreakBrush;
    private readonly SolidColorBrush? longBreakBrush;

    public FocusSessionPage()
    {
        InitializeComponent();
        focusBrush = Resources["FocusBrush"] as SolidColorBrush;
        shortBreakBrush = Resources["ShortBreakBrush"] as SolidColorBrush;
        longBreakBrush = Resources["LongBreakBrush"] as SolidColorBrush;
        Loaded += FocusSessionPage_Loaded;
        Unloaded += FocusSessionPage_Unloaded;
    }

    public FocusSessionViewModel ViewModel => FocusSessionViewModel.Instance;

    private void FocusSessionPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RebuildClock();
        UpdateMode();
        UpdatePlayIcon();

        clockGrid.SizeChanged += ClockGrid_SizeChanged;
    }

    private void FocusSessionPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        clockGrid.SizeChanged -= ClockGrid_SizeChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FocusSessionViewModel.IsRunning):
                UpdatePlayIcon();
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
        ringProgress.Stroke = ViewModel.Mode switch
        {
            FocusSessionMode.Focus => focusBrush,
            FocusSessionMode.ShortBreak => shortBreakBrush,
            FocusSessionMode.LongBreak => longBreakBrush,
            _ => null,
        };
    }

    private void UpdatePlayIcon()
    {
        playIcon.Glyph = ViewModel.IsRunning ? "\uE769" : "\uE768";
    }

    private void RebuildClock()
    {
        double size = GetClockSize();
        if (size <= 0) return;

        ringTrack.Width = size;
        ringTrack.Height = size;
        ringProgress.Width = size;
        ringProgress.Height = size;

        UpdateProgressArc();
    }

    private void UpdateProgressArc()
    {
        double size = GetClockSize();
        if (size <= 0) return;

        double thickness = RingThickness;
        if (size < 160)
        {
            thickness = RingThickness * (size / 160);
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
            clockGrid.MaxWidth = 320;
            clockGrid.MaxHeight = 320;
            timeTB.FontSize = 64;
            content.RowSpacing = 28;
            playButton.Width = 72;
            playButton.Height = 72;
            playButton.Padding = new(12);
            playButton.CornerRadius = new(36);
            playIcon.FontSize = 26;
            content.Margin = ContentMargin;
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
        }
    }
}
