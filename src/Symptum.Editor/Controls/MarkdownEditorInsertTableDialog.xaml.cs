using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Editor.Controls;

public sealed partial class MarkdownEditorInsertTableDialog : ContentDialog
{
    public EditorResult EditResult { get; private set; } = EditorResult.None;

    public string Markdown { get; private set; } = string.Empty;

    public ObservableCollection<MarkdownEditorTableColumn> Columns { get; } = [];

    public MarkdownEditorInsertTableDialog()
    {
        InitializeComponent();
        Opened += MarkdownEditorInsertTableDialog_Opened;
        PrimaryButtonClick += MarkdownEditorInsertTableDialog_PrimaryButtonClick;
        SecondaryButtonClick += MarkdownEditorInsertTableDialog_SecondaryButtonClick;
    }

    private void MarkdownEditorInsertTableDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        ModifyColumns(1);
        ModifyRows(1);
    }

    private void MarkdownEditorInsertTableDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        GenerateMarkdown();
        EditResult = EditorResult.Create;
        Reset();
    }

    private void MarkdownEditorInsertTableDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
        Reset();
    }

    private bool _reset = false;

    private void Reset()
    {
        _reset = true;
        rows = 0;
        Columns.Clear();
        columnsNB.Value = 1;
        rowsNB.Value = 1;
        _reset = false;
    }

    public async Task<EditorResult> CreateAsync()
    {
        await ShowAsync();
        return EditResult;
    }

    private void ColumnsNB_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_reset)
            ModifyColumns((int)args.NewValue);
    }

    private void RowsNB_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_reset)
            ModifyRows((int)args.NewValue);
    }

    private void GenerateMarkdown()
    {
        int rows = (int)rowsNB.Value;
        int columns = (int)columnsNB.Value;
        StringBuilder result = new();
        for (int r = 0; r < rows + 2; r++)
        {
            result.Append('|');
            for (int c = 0; c < columns; c++)
            {
                var column = Columns[c];
                bool isHeader = r == 0;
                bool isDivider = r == 1;
                if (isHeader)
                {
                    result.Append(' ');
                    string? columnHeader = column.Header;
                    result.Append(columnHeader);
                    result.Append(' ');
                }
                else if (isDivider)
                {
                    string divider = column.Alignment switch
                    {
                        1 => " :-: ",
                        2 => " --: ",
                        _ => " --- ",
                    };
                    result.Append(divider);
                }
                else
                {
                    result.Append(' ');
                    string? cellContent = Columns[c].Cells[r - 2].Content;
                    result.Append(cellContent);
                    result.Append(' ');
                }
                result.Append('|');
            }
            if (r < rows + 1) result.AppendLine();
        }

        Markdown = result.ToString();
    }

    private void ModifyColumns(int newCount)
    {
        int oldCount = Columns.Count;
        int diff = newCount - oldCount;
        if (diff > 0) // Add Columns
        {
            for (int i = 0; i < diff; i++)
            {
                MarkdownEditorTableColumn column = new()
                {
                    ColumnId = "Column " + (oldCount + i + 1)
                };
                ModifyCells(rows, column);
                Columns.Add(column);
            }
        }
        else // Remove Columns
        {
            for (int i = oldCount - 1; i >= newCount; i--)
            {
                Columns.RemoveAt(i);
            }
        }
    }

    int rows = 0;

    private void ModifyRows(int newCount)
    {
        foreach (var col in Columns)
        {
            ModifyCells(newCount, col);
        }

        rows = newCount;
    }

    private void ModifyCells(int newCount, MarkdownEditorTableColumn column)
    {
        int oldCount = column.Cells.Count;
        int diff = newCount - oldCount;
        if (diff > 0) // Add Cells
        {
            for (int i = 0; i < diff; i++)
            {
                MarkdownEditorTableCell cell = new()
                {
                    CellId = "Cell " + (oldCount + i + 1)
                };
                column.Cells.Add(cell);
            }
        }
        else // Remove Cells
        {
            for (int i = oldCount - 1; i >= newCount; i--)
            {
                column.Cells.RemoveAt(i);
            }
        }
    }
}

public partial class MarkdownEditorTableColumn : ObservableObject
{
    public string? ColumnId { get; set; }

    [ObservableProperty]
    public partial string Header { get; set; }

    public int Alignment { get; set; } = 0;

    public ObservableCollection<MarkdownEditorTableCell> Cells { get; private set; } = [];
}

public partial class MarkdownEditorTableCell : ObservableObject
{
    public string? CellId { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; }
}
