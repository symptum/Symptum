using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

public class HyperlinkElement : IAddChild
{
    private Hyperlink _hyperlink;
    private SInline inline;
    private LinkInline? _linkInline;
    private string? _baseUrl;
    private ILinkHandler? _linkHandler;
    private string? _url;

    public STextElement TextElement
    {
        get => inline;
    }

    public HyperlinkElement(LinkInline linkInline, string? baseUrl, MarkdownConfiguration config)
    {
        _baseUrl = baseUrl;
        string? url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _url = url;
        _linkInline = linkInline;
        _linkHandler = config.LinkHandler;
        _hyperlink = new Hyperlink();
        _hyperlink.Click += Hyperlink_Click;
        ToolTipService.SetToolTip(_hyperlink, linkInline.Title);
        inline = new()
        {
            Inline = _hyperlink
        };
    }

    private void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        _linkHandler?.HandleNavigation(_url, _baseUrl);
    }

    public void AddChild(IAddChild child)
    {
        if (child.TextElement is SInline inlineChild)
        {
            try
            {
                _hyperlink.Inlines.Add(inlineChild.Inline);
            }
            catch { }
        }
    }
}
