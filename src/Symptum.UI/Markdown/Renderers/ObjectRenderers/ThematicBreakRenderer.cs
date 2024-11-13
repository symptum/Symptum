// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

internal class ThematicBreakRenderer : WinUIObjectRenderer<ThematicBreakBlock>
{
    protected override void Write(WinUIRenderer renderer, ThematicBreakBlock obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        MyThematicBreak thematicBreak = new(obj);

        renderer.WriteBlock(thematicBreak);
    }
}
