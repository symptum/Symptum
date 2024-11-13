// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class DelimiterInlineRenderer : WinUIObjectRenderer<DelimiterInline>
{
    protected override void Write(WinUIRenderer renderer, DelimiterInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        // delimiter's children are emphasized text, we don't need to explicitly render them
        // Just need to render the children of the delimiter, I think..
        //renderer.WriteText(obj.ToLiteral());
        renderer.WriteChildren(obj);
    }
}
