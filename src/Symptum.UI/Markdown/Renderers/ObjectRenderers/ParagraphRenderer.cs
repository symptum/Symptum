// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

internal class ParagraphRenderer : WinUIObjectRenderer<ParagraphBlock>
{
    protected override void Write(WinUIRenderer renderer, ParagraphBlock obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        MyParagraph paragraph = new(obj);
        // set style
        renderer.Push(paragraph);
        renderer.WriteLeafInline(obj);
        renderer.Pop();
    }
}
