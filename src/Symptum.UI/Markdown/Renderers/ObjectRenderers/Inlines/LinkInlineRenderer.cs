// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class LinkInlineRenderer : WinUIObjectRenderer<LinkInline>
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
            MyImage image = new(link, Markdown.Extensions.GetUri(url, renderer.Config.BaseUrl), renderer.Config);
            renderer.WriteInline(image);
        }
        else
        {
            if (link.FirstChild is LinkInline linkInlineChild && linkInlineChild.IsImage)
            {
                renderer.Push(new MyHyperlinkButton(link, renderer.Config.BaseUrl));
            }
            else
            {
                MyHyperlink hyperlink = new(link, renderer.Config.BaseUrl);

                renderer.Push(hyperlink);
            }

            renderer.WriteChildren(link);
            renderer.Pop();
        }
    }
}
