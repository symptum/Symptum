using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.TextElements;

public class HyperlinkButtonElement : IAddChild
{
    private HyperlinkButton? _hyperLinkButton;
    private SContainer _container = new();
    private FlowDocumentElement? _flowDoc;
    private string? _baseUrl;
    private LinkInline? _linkInline;
    private MarkdownConfiguration _config;
    private ILinkHandler? _linkHandler;
    private string? _url;

    public STextElement TextElement
    {
        get => _container;
    }

    public HyperlinkButtonElement(LinkInline linkInline, string? baseUrl, MarkdownConfiguration config, ILinkHandler? linkHandler)
    {
        _baseUrl = baseUrl;
        _config = config;
        _url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _linkInline = linkInline;
        _linkHandler = linkHandler;

        _hyperLinkButton = new HyperlinkButton
        {
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        _hyperLinkButton.Click += (s, e) =>
        {
            _linkHandler?.HandleNavigation(_url, _baseUrl);
        };
        if (!string.IsNullOrWhiteSpace(linkInline.Title))
            ToolTipService.SetToolTip(_hyperLinkButton, linkInline.Title);
        else
            ToolTipService.SetToolTip(_hyperLinkButton, _url);

        if (_linkInline != null)
        {
            _flowDoc = new FlowDocumentElement(_config, false);
        }
        _hyperLinkButton.Content = _flowDoc?.StackPanel;
        _container.UIElement = _hyperLinkButton;
    }

    public void AddChild(IAddChild child)
    {
        _flowDoc?.AddChild(child);
    }
}
