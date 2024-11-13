// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.Alerts;
using Markdig.Syntax;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

internal class QuoteBlockRenderer : WinUIObjectRenderer<QuoteBlock>
{
    protected override void Write(WinUIRenderer renderer, QuoteBlock obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        MyQuote quote = new(obj, renderer.Config, (obj as AlertBlock)?.Kind);

        renderer.Push(quote);
        renderer.WriteChildren(obj);
        renderer.Pop();
    }
}
