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

    public STextElement TextElement
    {
        get => _container;
    }

    public HyperlinkButtonElement(LinkInline linkInline, string? baseUrl, MarkdownConfiguration config)
    {
        _config = config;
        _baseUrl = baseUrl;
        string? url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _linkInline = linkInline;
        Init(url, baseUrl);
    }

    private void Init(string? url, string? baseUrl)
    {
        _hyperLinkButton = new HyperlinkButton
        {
            NavigateUri = Extensions.GetUri(url, baseUrl),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        if (_linkInline != null)
        {
            _flowDoc = new FlowDocumentElement(_config, false);
        }
        _container.UIElement = _hyperLinkButton;
        _hyperLinkButton.Content = _flowDoc?.StackPanel;
    }

    public void AddChild(IAddChild child)
    {
        _flowDoc?.AddChild(child);
    }
}
