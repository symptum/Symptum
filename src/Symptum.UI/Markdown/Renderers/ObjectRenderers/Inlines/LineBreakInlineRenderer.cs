// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class LineBreakInlineRenderer : WinUIObjectRenderer<LineBreakInline>
{
    protected override void Write(WinUIRenderer renderer, LineBreakInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        if (obj.IsHard)
        {
            renderer.WriteInline(new MyLineBreak());
        }
        else
        {
            // Soft line break.
            renderer.WriteText(" ");
        }
    }
}
