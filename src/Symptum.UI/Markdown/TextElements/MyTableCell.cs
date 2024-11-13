// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.Tables;
using Microsoft.UI.Text;

namespace Symptum.UI.Markdown.TextElements;

internal class MyTableCell : IAddChild
{
    private TableCell _tableCell;
    private SParagraph _paragraph = new();
    private MyFlowDocument _flowDocument;
    private bool _isHeader;
    private int _columnIndex;
    private int _rowIndex;
    private Grid _container;

    public STextElement TextElement
    {
        get => _paragraph;
    }

    public Grid Container
    {
        get => _container;
    }

    public int ColumnSpan
    {
        get => _tableCell.ColumnSpan;
    }

    public int RowSpan
    {
        get => _tableCell.RowSpan;
    }

    public int ColumnIndex
    {
        get => _columnIndex;
    }

    public int RowIndex
    {
        get => _rowIndex;
    }

    public MyTableCell(TableCell tableCell, MarkdownConfig config, TextAlignment textAlignment, bool isHeader, int columnIndex, int rowIndex)
    {
        _isHeader = isHeader;
        _tableCell = tableCell;
        _columnIndex = columnIndex;
        _rowIndex = rowIndex;
        _container = new();

        _flowDocument = new MyFlowDocument(config, false);
        _flowDocument.StackPanel.HorizontalAlignment = textAlignment switch
        {
            TextAlignment.Left => HorizontalAlignment.Left,
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };

        _container.Padding = new Thickness(4);
        if (_isHeader)
        {
            _flowDocument.FontWeight = FontWeights.Bold;
        }
        _flowDocument.StackPanel.HorizontalAlignment = textAlignment switch
        {
            TextAlignment.Left => HorizontalAlignment.Left,
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };
        _container.Children.Add(_flowDocument.StackPanel);
    }

    public void AddChild(IAddChild child)
    {
        _flowDocument.AddChild(child);
    }
}
