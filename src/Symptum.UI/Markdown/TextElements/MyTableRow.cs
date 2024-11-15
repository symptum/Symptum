// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.Tables;

namespace Symptum.UI.Markdown.TextElements;

internal class MyTableRow : IAddChild
{
    private TableRow _tableRow;
    private StackPanel _stackPanel;
    private SContainer _container = new();

    public STextElement TextElement
    {
        get => _container;
    }

    public MyTableRow(TableRow tableRow)
    {
        _tableRow = tableRow;
        _stackPanel = new()
        {
            Orientation = Orientation.Horizontal
        };
        _container.UIElement = _stackPanel;
    }

    public void AddChild(IAddChild child)
    {
        if (child is MyTableCell cellChild)
        {
            //var richTextBlock = new RichTextBlock();
            //richTextBlock.Blocks.Add((Paragraph)cellChild.TextElement);
            //_stackPanel.Children.Add(richTextBlock);
        }
    }
}
