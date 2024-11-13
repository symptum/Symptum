// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class HtmlEntityInlineRenderer : WinUIObjectRenderer<HtmlEntityInline>
{
    protected override void Write(WinUIRenderer renderer, HtmlEntityInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        Markdig.Helpers.StringSlice transcoded = obj.Transcoded;
        renderer.WriteText(ref transcoded);
        // todo: wtf is this?
    }
}
