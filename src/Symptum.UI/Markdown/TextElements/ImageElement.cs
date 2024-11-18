using Markdig.Syntax.Inlines;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace Symptum.UI.Markdown.TextElements;

public class ImageElement : IAddChild
{
    private SContainer _container = new();
    private LinkInline? _linkInline;
    private Image _image = new();
    private Uri _uri;
    private IImageProvider? _imageProvider;
    private ISVGRenderer _svgRenderer;
    private double _precedentWidth;
    private double _precedentHeight;
    private bool _loaded;

    public STextElement TextElement
    {
        get => _container;
    }

    public ImageElement(LinkInline linkInline, Uri uri, MarkdownConfiguration config)
    {
        _linkInline = linkInline;
        _uri = uri;
        _imageProvider = config.ImageProvider;
        _svgRenderer = config.SVGRenderer ?? new DefaultSVGRenderer();
        Init();
        Size size = Extensions.GetMarkdownImageSize(linkInline);
        if (size.Width != 0)
        {
            _precedentWidth = size.Width;
        }
        if (size.Height != 0)
        {
            _precedentHeight = size.Height;
        }
    }

    private void Init()
    {
        _image.Loaded += LoadImage;
        _container.UIElement = _image;
    }

    private async void LoadImage(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        try
        {
            if (_imageProvider != null && _imageProvider.ShouldUseThisProvider(_uri.AbsoluteUri))
            {
                _image.Source = await _imageProvider.GetImageSource(_uri.AbsoluteUri);
            }
            else
            {
                HttpClient client = new();

                // Download data from URL
                HttpResponseMessage response = await client.GetAsync(_uri);

                string? contentType = response.Content.Headers.ContentType.MediaType;
                if (contentType == "image/svg+xml")
                {

                    string? svgString = await response.Content.ReadAsStringAsync();
                    ImageSource resImage = await _svgRenderer.SvgToImageSource(svgString);
                    if (resImage != null)
                    {
                        _image.Source = resImage;
                        Size size = Extensions.GetSvgSize(svgString);
                        if (size.Width > 0) _image.Width = size.Width;
                        if (size.Height > 0) _image.Height = size.Height;
                    }
                }
                else
                {
                    byte[] data = await response?.Content?.ReadAsByteArrayAsync();
                    // Create a BitmapImage for other supported formats
                    BitmapImage bitmap = new();
                    using (InMemoryRandomAccessStream stream = new())
                    {
                        // Write the data to the stream
                        await stream.WriteAsync(data.AsBuffer());
                        stream.Seek(0);

                        // Set the source of the BitmapImage
                        await bitmap.SetSourceAsync(stream);
                    }
                    _image.Source = bitmap;
                    _image.Width = bitmap.PixelWidth == 0 ? bitmap.DecodePixelWidth : bitmap.PixelWidth;
                    _image.Height = bitmap.PixelHeight == 0 ? bitmap.DecodePixelHeight : bitmap.PixelHeight;
                }

                _loaded = true;
            }

            if (_precedentWidth != 0)
            {
                _image.Width = _precedentWidth;
            }
            if (_precedentHeight != 0)
            {
                _image.Height = _precedentHeight;
            }
        }
        catch (Exception) { }
    }

    public void AddChild(IAddChild child) { }
}
