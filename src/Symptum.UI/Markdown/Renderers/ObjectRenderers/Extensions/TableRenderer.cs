// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.Tables;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Extensions;

public class TableRenderer : WinUIObjectRenderer<Table>
{
    protected override void Write(WinUIRenderer renderer, Table table)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(table);

        MyTable myTable = new(table);

        renderer.Push(myTable);

        for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
        {
            Markdig.Syntax.Block rowObj = table[rowIndex];
            TableRow row = (TableRow)rowObj;

            for (int i = 0; i < row.Count; i++)
            {
                Markdig.Syntax.Block cellObj = row[i];
                TableCell cell = (TableCell)cellObj;
                TextAlignment textAlignment = TextAlignment.Left;

                int columnIndex = i;

                if (table.ColumnDefinitions.Count > 0)
                {
                    columnIndex = cell.ColumnIndex < 0 || cell.ColumnIndex >= table.ColumnDefinitions.Count
                        ? i
                        : cell.ColumnIndex;
                    columnIndex = columnIndex >= table.ColumnDefinitions.Count ? table.ColumnDefinitions.Count - 1 : columnIndex;
                    TableColumnAlign? alignment = table.ColumnDefinitions[columnIndex].Alignment;
                    textAlignment = alignment switch
                    {
                        TableColumnAlign.Center => TextAlignment.Center,
                        TableColumnAlign.Left => TextAlignment.Left,
                        TableColumnAlign.Right => TextAlignment.Right,
                        _ => TextAlignment.Left,
                    };
                }

                MyTableCell myCell = new(cell, renderer.Config, textAlignment, row.IsHeader, columnIndex, rowIndex);

                renderer.Push(myCell);
                renderer.Write(cell);
                renderer.Pop();
            }
        }

        renderer.Pop();
    }
}
