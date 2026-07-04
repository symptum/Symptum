using Symptum.Common.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;

namespace Symptum.Pages;

public sealed partial class ImagePage : NavigablePage
{
    private ImageFileResource? _imageResource;

    public ImagePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public static readonly DependencyProperty ImageResourceProperty = DependencyProperty.Register(
        nameof(ImageResource),
        typeof(ImageFileResource),
        typeof(ImagePage),
        new(null));

    public ImageFileResource ImageResource
    {
        get => (ImageFileResource)GetValue(ImageResourceProperty);
        set => SetValue(ImageResourceProperty, value);
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is ImageFileResource imageResource)
        {
            ImageResource = imageResource;
            _imageResource = imageResource;
            placeholderText.Text = imageResource.FilePath ?? "No file path available";
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_imageResource == null) return;

        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(_imageResource.FilePath);
            using var stream = await file.OpenReadAsync();
            if (stream != null)
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                await bitmap.SetSourceAsync(stream);
                imagePreview.Source = bitmap;
                placeholderView.Visibility = Visibility.Collapsed;
                imagePreview.Visibility = Visibility.Visible;
                _imageSize = new(bitmap.PixelWidth, bitmap.PixelHeight);
                FitToView();
            }
        }
        catch
        {
            placeholderView.Visibility = Visibility.Visible;
            imagePreview.Visibility = Visibility.Collapsed;
            placeholderText.Text = "Image file not available for preview";
        }
    }

    private System.Numerics.Vector2 _imageSize;
    private float _currentZoom = 1.0f;
    private bool _isZooming;

    private void FitToView()
    {
        var available = imageScrollViewer.ActualSize;
        if (available.X > 0 && available.Y > 0 && _imageSize.X > 0 && _imageSize.Y > 0)
        {
            float fit = Math.Min(available.X / _imageSize.X, available.Y / _imageSize.Y);
            SetZoom(fit);
        }
    }

    private void SetZoom(float zoom)
    {
        _isZooming = true;
        _currentZoom = Math.Clamp(zoom, 0.1f, 4.0f);
        imagePreview.Width = _imageSize.X * _currentZoom;
        imagePreview.Height = _imageSize.Y * _currentZoom;
        zoomSlider.Value = _currentZoom * 100;
        zoomText.Text = $"{_currentZoom * 100:F0}%";
        _isZooming = false;
    }

    private void ZoomSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_isZooming)
            SetZoom((float)(e.NewValue / 100.0));
    }

    private void ZoomFitButton_Click(object sender, RoutedEventArgs e) => FitToView();

    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => SetZoom(_currentZoom + 0.25f);

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => SetZoom(_currentZoom - 0.25f);
}
