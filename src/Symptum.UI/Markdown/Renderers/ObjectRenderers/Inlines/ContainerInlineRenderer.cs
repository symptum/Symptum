// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class ContainerInlineRenderer : WinUIObjectRenderer<ContainerInline>
{
    protected override void Write(WinUIRenderer renderer, ContainerInline obj)
    {
        foreach (Inline inline in obj)
        {
            renderer.Write(inline);
        }
    }
}
