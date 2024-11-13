// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

internal class MyBlockContainer : IAddChild
{
    private ContainerBlock _containerBlock;
    private SContainer _container;
    private MyFlowDocument _flowDocument;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyBlockContainer(ContainerBlock containerBlock, MarkdownConfig config)
    {
        _containerBlock = containerBlock;
        _flowDocument = new MyFlowDocument(config, false);
        _container = new SContainer
        {
            UIElement = _flowDocument.StackPanel
        };
    }

    public void AddChild(IAddChild child)
    {
        _flowDocument.AddChild(child);
    }
}
