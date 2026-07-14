using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

public class LinkInlineRenderer : WinUIObjectRenderer<LinkInline>
{
    protected override void Write(WinUIRenderer renderer, LinkInline link)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(link);

        string? url = link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? link.Url : link.Url;

        if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
        {
            url = "#";
        }

        var control = renderer.MarkdownTextBlock;

        if (link.IsImage)
        {
            renderer.Push(new ImageElement(link, Helper.GetUri(url, control.BaseUrl), control));
        }
        else
        {
            if (link.FirstChild is LinkInline linkInlineChild && linkInlineChild.IsImage)
            {
                renderer.Push(new HyperlinkButtonElement(link, control.BaseUrl, control, renderer.LinkHandler));
            }
            else
            {
                renderer.Push(new HyperlinkElement(link, control.BaseUrl, renderer.LinkHandler));
            }
        }

        renderer.WriteChildren(link);
        renderer.Pop();
    }
}
