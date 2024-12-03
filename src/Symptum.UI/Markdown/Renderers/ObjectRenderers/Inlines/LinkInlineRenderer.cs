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

        if (link.IsImage)
        {
            ImageElement image = new(link, Markdown.Extensions.GetUri(url, renderer.Configuration.BaseUrl), renderer.Configuration);
            renderer.WriteInline(image);
        }
        else
        {
            if (link.FirstChild is LinkInline linkInlineChild && linkInlineChild.IsImage)
            {
                renderer.Push(new HyperlinkButtonElement(link, renderer.Configuration.BaseUrl, renderer.Configuration, renderer.LinkHandler));
            }
            else
            {
                HyperlinkElement hyperlink = new(link, renderer.Configuration.BaseUrl, renderer.LinkHandler);

                renderer.Push(hyperlink);
            }

            renderer.WriteChildren(link);
            renderer.Pop();
        }
    }
}
