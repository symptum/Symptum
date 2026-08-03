using Symptum.ViewModels;
using Windows.Foundation;

namespace Symptum.Pages;

public sealed partial class FocusSessionPage : NavigablePage
{
    private const double RingThickness = 14;

    public FocusSessionPage()
    {
        InitializeComponent();
        Loaded += FocusSessionPage_Loaded;
        Unloaded += FocusSessionPage_Unloaded;
    }

    public FocusSessionViewModel ViewModel => FocusSessionViewModel.Instance;

    private void FocusSessionPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RebuildClock();
        UpdateModeSelection();
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
                UpdateModeSelection();
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

    private void UpdateModeSelection()
    {
        modeSegmented.SelectedIndex = (int)ViewModel.Mode;
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
}
