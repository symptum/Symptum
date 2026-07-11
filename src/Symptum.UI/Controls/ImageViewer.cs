using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Symptum.Core.Helpers;
using Windows.System;
using Windows.UI.Core;

namespace Symptum.UI.Controls;

public partial class ImageViewer : Control
{
    private static readonly List<string> zoomLevels =
    [
        "800%",
        "700%",
        "600%",
        "500%",
        "400%",
        "300%",
        "200%",
        "100%",
        "75%",
        "50%",
        "25%",
        "12.5%",
    ];


    private ScrollViewer? _scrollViewer;
    private Image? _image;
    private Button? _actionButton;
    private FontIcon? _actionIcon;
    private Slider? _zoomSlider;
    private ComboBox? _zoomCombo;
    private TextBlock? _resText;
    private TextBlock? _sizeText;
    private Button? _zoomToFitButton;
    private RepeatButton? _zoomOutButton;
    private RepeatButton? _zoomInButton;

    private double _realWidth = 0;
    private double _realHeight = 0;
    private bool _internalUpdate = false;
    private double _currentZoom = 1;
    private bool _isPanning = false;
    private double _panStartX = 0;
    private double _panStartY = 0;
    private double _panStartScrollX = 0;
    private double _panStartScrollY = 0;

    public event EventHandler? ActionButtonClick;

    public ImageViewer()
    {
        DefaultStyleKey = typeof(ImageViewer);
        PreviewKeyDown += ImageViewer_KeyDown;
        IsTabStop = true;
        Unloaded += ImageViewer_Unloaded;
    }

    #region Properties


    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(ImageViewer), new PropertyMetadata(null, OnSourceChanged));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageViewer v && e.NewValue is ImageSource src)
        {
            v._image?.Source = src;
            v.ProcessImage();
        }
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty FileSizeProperty = DependencyProperty.Register(
        nameof(FileSize), typeof(ulong), typeof(ImageViewer), new PropertyMetadata(0UL));

    public ulong FileSize
    {
        get => (ulong)GetValue(FileSizeProperty);
        set => SetValue(FileSizeProperty, value);
    }

    public static readonly DependencyProperty ActionButtonGlyphProperty = DependencyProperty.Register(
        nameof(ActionButtonGlyph), typeof(string), typeof(ImageViewer), new PropertyMetadata(null, OnActionButtonGlyphChanged));

    private static void OnActionButtonGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        if (d is ImageViewer viewer && args.NewValue is string glyph)
        {
            viewer._actionIcon?.Glyph = glyph;
        }
    }

    public string ActionButtonGlyph
    {
        get => (string)GetValue(ActionButtonGlyphProperty);
        set => SetValue(ActionButtonGlyphProperty, value);
    }

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UnsubscribeFromTemplateEvents();

        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        _image = GetTemplateChild("PART_Image") as Image;
        _actionButton = GetTemplateChild("PART_ActionButton") as Button;
        _actionIcon = GetTemplateChild("PART_ActionButtonIcon") as FontIcon;
        _zoomSlider = GetTemplateChild("PART_ZoomSlider") as Slider;
        _zoomCombo = GetTemplateChild("PART_ZoomCombo") as ComboBox;
        _resText = GetTemplateChild("PART_ResText") as TextBlock;
        _sizeText = GetTemplateChild("PART_SizeText") as TextBlock;
        _zoomToFitButton = GetTemplateChild("PART_ZoomToFitButton") as Button;
        _zoomOutButton = GetTemplateChild("PART_ZoomOutButton") as RepeatButton;
        _zoomInButton = GetTemplateChild("PART_ZoomInButton") as RepeatButton;

        if (_image != null)
        {
            _image.ImageOpened += Image_ImageOpened;
            _image.Source = Source;
            _image.PointerPressed += Image_PointerPressed;
            _image.PointerMoved += Image_PointerMoved;
            _image.PointerReleased += Image_PointerReleased;
            _image.ManipulationMode = ManipulationModes.Scale;
            _image.ManipulationDelta += Image_ManipulationDelta;
            _image.PreviewKeyDown += ImageViewer_KeyDown;
        }

        if (_scrollViewer != null)
        {
            _scrollViewer.PointerWheelChanged += ScrollViewer_PointerWheelChanged;
            _scrollViewer.PreviewKeyDown += ImageViewer_KeyDown;
        }

        _actionButton?.Click += ActionButton_Click;
        _actionIcon?.Glyph = ActionButtonGlyph;

        if (_zoomSlider != null)
        {
            _zoomSlider.ValueChanged += ZoomSlider_ValueChanged;
            _zoomSlider.Value = _currentZoom * 100.0;
        }

        if (_zoomCombo != null)
        {
            _zoomCombo?.ItemsSource = zoomLevels;
            _zoomCombo?.SelectionChanged += ZoomCombo_SelectionChanged;
        }

        _zoomToFitButton?.Click += ZoomToFitButton_Click;
        _zoomOutButton?.Click += ZoomOutButton_Click;
        _zoomInButton?.Click += ZoomInButton_Click;

        Focus(FocusState.Keyboard);

        UpdateInfoTexts();
    }

    private void UnsubscribeFromTemplateEvents()
    {
        if (_image != null)
        {
            _image.ImageOpened -= Image_ImageOpened;
            _image.PointerPressed -= Image_PointerPressed;
            _image.PointerMoved -= Image_PointerMoved;
            _image.PointerReleased -= Image_PointerReleased;
            _image.ManipulationDelta -= Image_ManipulationDelta;
            _image.PreviewKeyDown -= ImageViewer_KeyDown;
        }

        if (_scrollViewer != null)
        {
            _scrollViewer.PointerWheelChanged -= ScrollViewer_PointerWheelChanged;
            _scrollViewer.PreviewKeyDown -= ImageViewer_KeyDown;
        }

        _actionButton?.Click -= ActionButton_Click;
        _zoomSlider?.ValueChanged -= ZoomSlider_ValueChanged;
        _zoomCombo?.SelectionChanged -= ZoomCombo_SelectionChanged;
        _zoomToFitButton?.Click -= ZoomToFitButton_Click;
        _zoomOutButton?.Click -= ZoomOutButton_Click;
        _zoomInButton?.Click -= ZoomInButton_Click;
    }

    private void ImageViewer_Unloaded(object sender, RoutedEventArgs e)
    {
        PreviewKeyDown -= ImageViewer_KeyDown;
        Unloaded -= ImageViewer_Unloaded;

        UnsubscribeFromTemplateEvents();

        _scrollViewer = null;
        _image = null;
        _zoomSlider = null;
        _zoomCombo = null;
        _resText = null;
        _sizeText = null;
        _zoomToFitButton = null;
        _zoomOutButton = null;
        _zoomInButton = null;
    }

    private void ZoomToFitButton_Click(object sender, RoutedEventArgs e) => FitToView();
    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => SetZoom(DecreasedZoom());
    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => SetZoom(IncreasedZoom());

    private void Image_ImageOpened(object sender, RoutedEventArgs e)
    {
        ProcessImage();
    }

    private void ProcessImage()
    {
        if (_image?.Source is BitmapImage bi)
        {
            _realWidth = bi.PixelWidth != 0 ? bi.PixelWidth : bi.DecodePixelWidth;
            _realHeight = bi.PixelHeight != 0 ? bi.PixelHeight : bi.DecodePixelHeight;
        }
        else
        {
            _realWidth = _scrollViewer?.ActualWidth ?? 0;
            _realHeight = _scrollViewer?.ActualHeight ?? 0;
        }

        UpdateInfoTexts();
        FitToView();
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e) => ActionButtonClick?.Invoke(this, EventArgs.Empty);

    private void ZoomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_internalUpdate) return;
        if (_zoomCombo?.SelectedItem is string s)
        {
            if (s.EndsWith('%')) s = s[..^1];
            if (double.TryParse(s, out var num))
            {
                SetZoom(num / 100.0);
            }
        }
    }

    private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_internalUpdate) return;
        SetZoom(e.NewValue / 100.0);
    }

    private void UpdateInfoTexts()
    {
        _resText?.Text = $"{(int)_realWidth} x {(int)_realHeight}";
        _sizeText?.Text = FileHelper.FormatSize((ulong)FileSize);
    }

    private void SetZoom(double z)
    {
        z = Math.Clamp(z, 0.1, 10);
        _internalUpdate = true;
        if (_image != null && _realWidth > 0 && _realHeight > 0)
        {
            _image.Width = _realWidth * z;
            _image.Height = _realHeight * z;
        }
        _zoomSlider?.Value = z * 100.0;
        _zoomCombo?.Text = $"{z * 100:0}%";
        CenterZoom(z);
        _currentZoom = z;
        _internalUpdate = false;
    }

    private void FitToView()
    {
        if (_scrollViewer == null || _image == null || _realWidth == 0 || _realHeight == 0) return;
        var available = _scrollViewer.ActualSize;
        if (available.X <= 0 || available.Y <= 0) return;
        double fit = Math.Min(available.X / _realWidth, available.Y / _realHeight);
        SetZoom(fit);
    }

    private void CenterZoom(double newZoom)
    {
        if (_scrollViewer == null || _image == null) return;

        double viewportWidth = _scrollViewer.ActualWidth;
        double viewportHeight = _scrollViewer.ActualHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0) return;

        double currentHorizontalOffset = _scrollViewer.HorizontalOffset;
        double currentVerticalOffset = _scrollViewer.VerticalOffset;

        double centerX = (currentHorizontalOffset + viewportWidth / 2.0) / (_realWidth * _currentZoom);
        double centerY = (currentVerticalOffset + viewportHeight / 2.0) / (_realHeight * _currentZoom);

        double newImageWidth = _realWidth * newZoom;
        double newImageHeight = _realHeight * newZoom;
        double newHorizontalOffset = (centerX * newImageWidth) - (viewportWidth / 2.0);
        double newVerticalOffset = (centerY * newImageHeight) - (viewportHeight / 2.0);

        newHorizontalOffset = Math.Max(0, Math.Min(newHorizontalOffset, newImageWidth - viewportWidth));
        newVerticalOffset = Math.Max(0, Math.Min(newVerticalOffset, newImageHeight - viewportHeight));

        _scrollViewer.ScrollToHorizontalOffset(newHorizontalOffset);
        _scrollViewer.ScrollToVerticalOffset(newVerticalOffset);
    }

    private double IncreasedZoom() => _currentZoom switch
    {
        >= 6.0 => _currentZoom + 0.5,
        >= 2.0 => _currentZoom + 0.25,
        _ => _currentZoom + 0.1
    };

    private double DecreasedZoom() => _currentZoom switch
    {
        <= 2.0 => _currentZoom - 0.1,
        <= 6.0 => _currentZoom - 0.25,
        _ => _currentZoom - 0.5
    };

    private void Image_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_scrollViewer == null) return;

        var point = e.GetCurrentPoint((UIElement)sender);

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsMiddleButtonPressed ||
            e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
        {
            _isPanning = true;
            _panStartX = point.Position.X;
            _panStartY = point.Position.Y;
            _panStartScrollX = _scrollViewer.HorizontalOffset;
            _panStartScrollY = _scrollViewer.VerticalOffset;
            ((UIElement)sender).CapturePointer(e.Pointer);
        }
    }

    private void Image_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPanning || _scrollViewer == null || _image == null) return;

        var point = e.GetCurrentPoint((UIElement)sender);
        double deltaX = _panStartX - point.Position.X;
        double deltaY = _panStartY - point.Position.Y;

        double newScrollX = _panStartScrollX + deltaX;
        double newScrollY = _panStartScrollY + deltaY;

        double maxScrollX = _image.Width - _scrollViewer.ActualWidth;
        double maxScrollY = _image.Height - _scrollViewer.ActualHeight;

        newScrollX = Math.Max(0, Math.Min(newScrollX, maxScrollX));
        newScrollY = Math.Max(0, Math.Min(newScrollY, maxScrollY));

        _scrollViewer.ScrollToHorizontalOffset(newScrollX);
        _scrollViewer.ScrollToVerticalOffset(newScrollY);
    }

    private void Image_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        }
    }

    private void Image_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        // Pinch-to-zoom support for touchpad and touch
        if (e.Delta.Scale != 1.0)
        {
            double scaleFactor = e.Delta.Scale;
            double newZoom = _currentZoom * scaleFactor;
            SetZoom(newZoom);
            e.Handled = true;
        }
    }

    private void ScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(_scrollViewer);
        var properties = point.Properties;

        // Ctrl + Scroll to zoom
        if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
            & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
        {
            int wheelDelta = properties.MouseWheelDelta;
            double zoomDelta = wheelDelta > 0 ? 0.1 : -0.1;
            SetZoom(_currentZoom + zoomDelta);
            e.Handled = true;
        }
    }

    private void ImageViewer_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_scrollViewer == null) return;

        switch (e.Key)
        {
            // Zoom shortcuts
            case VirtualKey.Add:
            case (VirtualKey)0xBB:
                SetZoom(IncreasedZoom());
                e.Handled = true;
                break;

            case VirtualKey.Subtract:
            case (VirtualKey)0xBD:
                SetZoom(DecreasedZoom());
                e.Handled = true;
                break;

            case VirtualKey.Number0:
                if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                    & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down)
                {
                    FitToView();
                    e.Handled = true;
                }
                break;

            // Arrow keys for panning
            case VirtualKey.Left:
                _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - 20);
                e.Handled = true;
                break;

            case VirtualKey.Right:
                _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + 20);
                e.Handled = true;
                break;

            case VirtualKey.Up:
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - 20);
                e.Handled = true;
                break;

            case VirtualKey.Down:
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + 20);
                e.Handled = true;
                break;

            case VirtualKey.Home:
                _scrollViewer.ScrollToHorizontalOffset(0);
                _scrollViewer.ScrollToVerticalOffset(0);
                e.Handled = true;
                break;

            case VirtualKey.End:
                _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.ScrollableWidth);
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.ScrollableHeight);
                e.Handled = true;
                break;
        }
    }
}
