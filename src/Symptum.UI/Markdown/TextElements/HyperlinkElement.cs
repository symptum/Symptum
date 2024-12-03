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

    public HyperlinkElement(LinkInline linkInline, string? baseUrl, ILinkHandler? linkHandler)
    {
        _baseUrl = baseUrl;
        _url = linkInline.GetDynamicUrl != null ? linkInline.GetDynamicUrl() ?? linkInline.Url : linkInline.Url;
        _linkInline = linkInline;
        _linkHandler = linkHandler;

        _hyperlink = new Hyperlink();
        _hyperlink.Click += (s, e) =>
        {
            _linkHandler?.HandleNavigation(_url, _baseUrl);
        };

        if (!string.IsNullOrWhiteSpace(linkInline.Title))
            ToolTipService.SetToolTip(_hyperlink, linkInline.Title);
        else
            ToolTipService.SetToolTip(_hyperlink, _url);

        inline = new()
        {
            Inline = _hyperlink
        };
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
