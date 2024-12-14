using System.Numerics;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Editor.EditorPages;

public sealed partial class ImageViewerPage : EditorPageBase
{
    private ImageFileResource? _imageFileResource;
    private ResourcePropertiesEditorDialog propertyEditorDialog = new();
    private List<string> zoomLevels =
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

    public ImageViewerPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.PicturesIconSource;
        zoomCB.ItemsSource = zoomLevels;
        Loaded += ImageViewerPage_Loaded;
    }

    private async void PropsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imageFileResource != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(_imageFileResource);
            if (result == EditorResult.Update)
                HasUnsavedChanges = true;
        }
    }

    private bool _loaded = false;

    private async void ImageViewerPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded || EditableContent is not ImageFileResource imageFileResource) return;

        _imageFileResource = imageFileResource;

        var stream = await ResourceHelper.OpenFileForReadAsync(imageFileResource);
        if (stream == null) return;

        Vector2 availableSize = scrollViewer.ActualSize;
        if (SvgFileExtension.Equals(imageFileResource.FileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            SvgImageSource svg = new();
            await svg.SetSourceAsync(stream);
            imagePreview.Source = svg;
            _imageSize = availableSize;
        }
        else
        {
            // NOTE: IRandomAccessStream doesn't seem to render in WASM?
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream);
            imagePreview.Source = bitmap;
            int width = bitmap.PixelWidth != 0 ? bitmap.PixelWidth : bitmap.DecodePixelWidth;
            int height = bitmap.PixelHeight != 0 ? bitmap.PixelHeight : bitmap.DecodePixelHeight;
            _imageSize = new(width, height);
        }

        sizeTB.Text = FormatSize(stream.Size);
        CalculateZoomFactor(availableSize, _imageSize);
        resTB.Text = $"{_imageSize.X} x {_imageSize.Y}";
        scrollViewer.ScrollToHorizontalOffset(_imageSize.X);
        scrollViewer.ScrollToVerticalOffset(_imageSize.Y);
        _loaded = true;
    }

    private Vector2 _imageSize;
    private float fitZoomFactor = 1.0f;
    private float currentZoomFactor = 1.0f;
    private bool _zooming = false;

    private void CalculateZoomFactor(Vector2 availableSize, Vector2 requiredSize)
    {
        fitZoomFactor = Math.Min(availableSize.X / requiredSize.X, availableSize.Y / requiredSize.Y);
        SetZoom(fitZoomFactor);
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(DecreasedZoom());
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(IncreasedZoom());
    }

    private float IncreasedZoom() => currentZoomFactor switch
    {
        >= 6.0f => currentZoomFactor + 0.5f,
        >= 2.0f => currentZoomFactor + 0.25f,
        _ => currentZoomFactor + 0.1f
    };

    private float DecreasedZoom() => currentZoomFactor switch
    {
        <= 2.0f => currentZoomFactor - 0.1f, // NOTE: floats are messy :(
        <= 6.0f => currentZoomFactor - 0.25f,
        _ => currentZoomFactor - 0.5f
    };

    private void SetZoom(float zoom)
    {
        zoom = (float)Math.Clamp(zoom, 0.1, 10);
        _zooming = true;

        imagePreview.Width = _imageSize.X * zoom;
        imagePreview.Height = _imageSize.Y * zoom;
        imagePreview.Stretch = Stretch.Uniform;
        currentZoomFactor = zoom;
        zoomCB.Text = currentZoomFactor.ToString("0%");
        if (!float.IsNaN(currentZoomFactor)) zoomSlider.Value = currentZoomFactor * 100.0;
        _zooming = false;
    }

    private void ZoomCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (zoomCB.SelectedItem is string value)
        {
            value = value[..^1];
            if (double.TryParse(value, out double num))
            {
                SetZoom((float)(num / 100.0));
            }
        }
    }

    private void ZoomFitButton_Click(object sender, RoutedEventArgs e)
    {
        CalculateZoomFactor(scrollViewer.ActualSize, _imageSize);
    }

    private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_zooming) return;
        SetZoom((float)(zoomSlider.Value / 100.0));
    }
}
