// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.Tables;
using Microsoft.UI;

namespace Symptum.UI.Markdown.TextElements;

internal class MyTable : IAddChild
{
    private Table _table;
    private SContainer _container;
    private MyTableUIElement _tableElement;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyTable(Table table)
    {
        _table = table;
        _container = new();
        int column = table.FirstOrDefault() is TableRow row ? row.Count : 0;

        _tableElement = new MyTableUIElement
        (
            column,
            table.Count,
            3,
            new SolidColorBrush(Colors.Gray)
        )
        { HorizontalAlignment = HorizontalAlignment.Center };
        _container.UIElement = _tableElement;
    }

    public void AddChild(IAddChild child)
    {
        if (child is MyTableCell cellChild)
        {
            Grid cell = cellChild.Container;

            Grid.SetColumn(cell, cellChild.ColumnIndex);
            Grid.SetRow(cell, cellChild.RowIndex);
            Grid.SetColumnSpan(cell, cellChild.ColumnSpan);
            Grid.SetRowSpan(cell, cellChild.RowSpan);

            _tableElement.Children.Add(cell);
        }
    }
}
