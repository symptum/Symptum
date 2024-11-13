// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class AutoLinkInlineRenderer : WinUIObjectRenderer<AutolinkInline>
{
    protected override void Write(WinUIRenderer renderer, AutolinkInline link)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(link);

        string url = link.Url;
        if (link.IsEmail)
        {
            url = "mailto:" + url;
        }

        if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
        {
            url = "#";
        }

        MyAutolinkInline autolink = new(link);

        renderer.Push(autolink);

        renderer.WriteText(link.Url);
        renderer.Pop();
    }
}
