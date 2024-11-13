// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

internal class ListRenderer : WinUIObjectRenderer<ListBlock>
{
    protected override void Write(WinUIRenderer renderer, ListBlock listBlock)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(listBlock);

        MyList list = new(listBlock, renderer.Config, listBlock.Parent is MarkdownDocument);

        renderer.Push(list);

        foreach (Block item in listBlock)
        {
            ListItemBlock listItemBlock = (ListItemBlock)item;
            MyBlockContainer listItem = new(listItemBlock, renderer.Config);
            renderer.Push(listItem);
            renderer.WriteChildren(listItemBlock);
            renderer.Pop();
        }

        renderer.Pop();
    }
}
