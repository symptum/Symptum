using HtmlAgilityPack;
using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

public class HyperlinkElement : IAddChild
{
    private Hyperlink _hyperlink;
    private SInline inline;
    private LinkInline? _linkInline;
    private HtmlNode? _htmlNode;

    public STextElement TextElement => inline;

    public HyperlinkElement(LinkInline linkInline, string? baseUrl, ILinkHandler? linkHandler) :
        this(linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url,
            baseUrl, linkHandler, linkInline.Title, linkInline)
    { }

    public HyperlinkElement(HtmlNode htmlNode, string? baseUrl, ILinkHandler? linkHandler) :
        this(htmlNode.GetAttribute("href", "#"), baseUrl, linkHandler, htmlNode.InnerText, htmlNode: htmlNode)
    { }

    private HyperlinkElement(string? url, string? baseUrl, ILinkHandler? linkHandler, string? title, LinkInline? linkInline = null, HtmlNode? htmlNode = null)
    {
        _linkInline = linkInline;
        _htmlNode = htmlNode;

        var weakHandler = linkHandler != null ? new WeakReference<ILinkHandler>(linkHandler) : null;
        string? capturedUrl = url;
        string? capturedBaseUrl = baseUrl;

        _hyperlink = new Hyperlink();
        _hyperlink.Click += (s, e) =>
        {
            if (weakHandler != null && weakHandler.TryGetTarget(out var handler))
                handler.HandleNavigation(capturedUrl, capturedBaseUrl);
        };

        if (!string.IsNullOrWhiteSpace(title))
            ToolTipService.SetToolTip(_hyperlink, title);
        else
            ToolTipService.SetToolTip(_hyperlink, url);

        inline = new()
        {
            Inline = _hyperlink
        };
    }

    public void AddChild(IAddChild child)
    {
        if (child.TextElement is SInline inlineChild)
        {
            _hyperlink.Inlines.Add(inlineChild.Inline);
        }
    }
}
