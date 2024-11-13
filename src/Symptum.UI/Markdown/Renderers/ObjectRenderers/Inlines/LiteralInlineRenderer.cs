// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class LiteralInlineRenderer : WinUIObjectRenderer<LiteralInline>
{
    protected override void Write(WinUIRenderer renderer, LiteralInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        if (obj.Content.IsEmpty)
            return;

        renderer.WriteText(ref obj.Content);
    }
}
