using System.Diagnostics;
using System.Globalization;
using HtmlAgilityPack;
using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml.Media.Imaging;
using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Symptum.UI.Markdown.TextElements;

public class ImageElement : IAddChild
{
    private SContainer _container = new();
    private LinkInline? _linkInline;
    private HtmlNode? _htmlNode;
    private Image _image = new();
    private Uri _uri;
    private IImageProvider? _imageProvider;
    private ISVGRenderer _svgRenderer;
    private double _precedentWidth;
    private double _precedentHeight;
    private bool _loaded;

    private TextBlock _altText;

    private const int MaxCacheSize = 200;
    private static readonly Dictionary<Uri, (ImageSource Source, long LastAccess)> _imageCache = new();
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly DefaultSVGRenderer _defaultSVGRenderer = new();
    private static readonly object _cacheLock = new();

    public STextElement TextElement => _container;

    public ImageElement(LinkInline linkInline, Uri uri, MarkdownTextBlock control)
    {
        _linkInline = linkInline;
        _uri = uri;
        _imageProvider = control.ImageProvider;
        _svgRenderer = control.SVGRenderer ?? _defaultSVGRenderer;
        Init(linkInline.Label, control);
        Size size = Helper.GetMarkdownImageSize(linkInline);
        if (size.Width != 0)
        {
            _precedentWidth = size.Width;
        }
        if (size.Height != 0)
        {
            _precedentHeight = size.Height;
        }
    }

    public ImageElement(HtmlNode htmlNode, MarkdownTextBlock control)
    {
        if (Uri.TryCreate(htmlNode.GetAttribute("src", "#"), UriKind.RelativeOrAbsolute, out Uri? uri))
            _uri = uri;

        _htmlNode = htmlNode;
        _imageProvider = control.ImageProvider;
        _svgRenderer = control.SVGRenderer ?? _defaultSVGRenderer;
        Init(htmlNode.GetAttribute("alt", string.Empty), control);
        int.TryParse(htmlNode.GetAttribute("width", "0"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var width);

        int.TryParse(htmlNode.GetAttribute("height", "0"),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var height);

        if (width > 0)
        {
            _precedentWidth = width;
        }
        if (height > 0)
        {
            _precedentHeight = height;
        }
    }

    private void Init(string? altText, MarkdownTextBlock control)
    {
        _image.Stretch = Stretch.Uniform;
        _image.Loaded += OnImageLoaded;
        _image.Unloaded += OnImageUnloaded;
        Grid _grid = new()
        {
            RowSpacing = 4
        };
        _grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Auto) });
        _grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Auto) });
        _grid.RowDefinitions.Add(new() { Height = new(0, GridUnitType.Auto) });

        _altText = new()
        {
            Text = altText,
            Style = control.BodyTextBlockStyle
        };
        _altText.SetValue(Grid.RowProperty, 1);
        _grid.Children.Add(_altText);
        _grid.Children.Add(_image);
        _container.UIElement = _grid;
        if (_linkInline != null && !string.IsNullOrWhiteSpace(_linkInline.Title))
        {
            ToolTipService.SetToolTip(_grid, _linkInline.Title);
            TextBlock _titleTB = new()
            {
                Text = _linkInline.Title,
                Style = control.BodyTextBlockStyle,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _titleTB.SetValue(Grid.RowProperty, 2);
            _grid.Children.Add(_titleTB);
        }
    }

    private void OnImageLoaded(object sender, RoutedEventArgs e)
    {
        _image.Loaded -= OnImageLoaded;
        _ = LoadImageAsync();
    }

    private void OnImageUnloaded(object sender, RoutedEventArgs e)
    {
        _image.Loaded -= OnImageLoaded;
        _image.Unloaded -= OnImageUnloaded;
    }

    private static ImageSource? TryGetCached(Uri uri)
    {
        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(uri, out var entry))
            {
                _imageCache[uri] = (entry.Source, Environment.TickCount64);
                return entry.Source;
            }
        }
        return null;
    }

    private static void AddToCache(Uri uri, ImageSource source)
    {
        lock (_cacheLock)
        {
            _imageCache[uri] = (source, Environment.TickCount64);

            // Evict oldest entries when over capacity
            if (_imageCache.Count > MaxCacheSize)
            {
                var oldest = _imageCache
                    .OrderBy(kvp => kvp.Value.LastAccess)
                    .Take(_imageCache.Count - MaxCacheSize)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldest)
                    _imageCache.Remove(key);
            }
        }
    }

    private async Task LoadImageAsync()
    {
        if (_loaded) return;

        void imageLoaded(ImageSource source)
        {
            _loaded = true;
            AddToCache(_uri, source);
            _altText.Visibility = Visibility.Collapsed;
        }

        if (TryGetCached(_uri) is ImageSource value)
        {
            _image.Source = value;
            imageLoaded(value);
        }
        else
        {
            try
            {
                if (_uri.Scheme == "symptum" && ResourceManager.TryGetResourceByUri(_uri, out IResource? resource) &&
                    resource is ImageFileResource imgRes)
                {
                    var (src, _) = await ImageResourceHelper.GetImageFromResource(imgRes);
                    if (src != null)
                    {
                        _image.Source = src;
                        SetImageSize(src);
                        imageLoaded(src);
                    }
                }
                else if (_imageProvider != null && _imageProvider.ShouldUseThisProvider(_uri.AbsoluteUri))
                {
                    var source = await _imageProvider.GetImageSource(_uri.AbsoluteUri);
                    _image.Source = source;
                    imageLoaded(source);
                }
                else if (_uri.Scheme == "file")
                {
                    StorageFile? file = await StorageFile.GetFileFromPathAsync(_uri.LocalPath);
                    if (file != null)
                    {
                        using IRandomAccessStream? stream = await file.OpenAsync(FileAccessMode.Read);
                        BitmapImage bitmap = new();
                        if (stream != null) await bitmap.SetSourceAsync(stream);
                        _image.Source = bitmap;
                        _image.Width = bitmap.PixelWidth == 0 ? bitmap.DecodePixelWidth : bitmap.PixelWidth;
                        _image.Height = bitmap.PixelHeight == 0 ? bitmap.DecodePixelHeight : bitmap.PixelHeight;
                        imageLoaded(bitmap);
                    }
                }
                else
                {
                    HttpResponseMessage response = await _client.GetAsync(_uri);
                    if (response != null)
                    {
                        string? contentType = response.Content.Headers?.ContentType?.MediaType;
                        if (contentType == "image/svg+xml")
                        {
                            string? svgString = await response.Content.ReadAsStringAsync();
                            Size size = Helper.GetSvgSize(svgString);
                            ImageSource resImage = await _svgRenderer.SvgToImageSource(svgString);
                            if (resImage != null)
                            {
                                _image.Source = resImage;
                                SetImageSize(resImage, size);
                                imageLoaded(resImage);
                            }
                        }
                        else
                        {
                            using Stream? stream = await response.Content.ReadAsStreamAsync();
                            BitmapImage bitmap = new();

                            if (stream != null) await bitmap.SetSourceAsync(stream.AsRandomAccessStream());

                            _image.Source = bitmap;
                            SetImageSize(bitmap);
                            imageLoaded(bitmap);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Image load failed for {_uri}: {ex.Message}");
            }
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

    private void SetImageSize(ImageSource src, Size size = default)
    {
        if (src is BitmapImage bitmap)
        {
            _image.Width = bitmap.PixelWidth == 0 ? bitmap.DecodePixelWidth : bitmap.PixelWidth;
            _image.Height = bitmap.PixelHeight == 0 ? bitmap.DecodePixelHeight : bitmap.PixelHeight;
        }
        else if (src is SvgImageSource)
        {
            _image.Width = size.Width;
            _image.Height = size.Height;
        }
    }

    public void AddChild(IAddChild child)
    {
        if (child != null && child.TextElement is SInline inline)
        {
            _altText.Inlines.Add(inline.Inline);
        }
    }
}
