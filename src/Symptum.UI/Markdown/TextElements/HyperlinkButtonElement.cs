using HtmlAgilityPack;
using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.TextElements;

public class HyperlinkButtonElement : IAddChild
{
    private HyperlinkButton? _hyperLinkButton;
    private SContainer _container = new();
    private FlowDocumentElement? _flowDoc;
    private string? _baseUrl;
    private LinkInline? _linkInline;
    private HtmlNode? _htmlNode;
    private MarkdownTextBlock _control;
    private ILinkHandler? _linkHandler;
    private string? _url;

    public STextElement TextElement => _container;

    public HyperlinkButtonElement(LinkInline linkInline, string? baseUrl, MarkdownTextBlock control, ILinkHandler? linkHandler) :
        this(linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url,
            baseUrl, control, linkHandler, linkInline.Title, linkInline)
    { }

    public HyperlinkButtonElement(HtmlNode htmlNode, string? baseUrl, MarkdownTextBlock control, ILinkHandler? linkHandler) :
        this(htmlNode.GetAttribute("href", "#"), baseUrl, control, linkHandler, htmlNode.InnerText, htmlNode: htmlNode)
    { }

    private HyperlinkButtonElement(string? url, string? baseUrl, MarkdownTextBlock control, ILinkHandler? linkHandler,
        string? title, LinkInline? linkInline = null, HtmlNode? htmlNode = null)
    {
        _linkInline = linkInline;
        _htmlNode = htmlNode;
        _baseUrl = baseUrl;
        _control = control;
        _url = url;
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

        if (!string.IsNullOrWhiteSpace(title))
            ToolTipService.SetToolTip(_hyperLinkButton, title);
        else
            ToolTipService.SetToolTip(_hyperLinkButton, _url);

        _flowDoc = new FlowDocumentElement(_control, false);
        _hyperLinkButton.Content = _flowDoc?.StackPanel;
        _container.UIElement = _hyperLinkButton;
    }

    public void AddChild(IAddChild child)
    {
        _flowDoc?.AddChild(child);
    }
}
