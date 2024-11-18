using Markdig.Extensions.Tables;

namespace Symptum.UI.Markdown.TextElements;

public class TableCellElement : IAddChild
{
    private TableCell _tableCell;
    private FlowDocumentElement _flowDocument;
    private bool _isHeader;
    private int _columnIndex;
    private int _rowIndex;
    private Grid _grid;
    private SContainer _container = new();

    public STextElement TextElement
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

    public TableCellElement(TableCell tableCell, MarkdownConfiguration config, TextAlignment textAlignment, bool isHeader, int columnIndex, int rowIndex)
    {
        _isHeader = isHeader;
        _tableCell = tableCell;
        _columnIndex = columnIndex;
        _rowIndex = rowIndex;

        _flowDocument = new FlowDocumentElement(config, false);

        if (isHeader)
            _flowDocument.TextBlockStyle = config.Themes.TableHeaderTextBlockStyle;

        _flowDocument.StackPanel.HorizontalAlignment = textAlignment switch
        {
            TextAlignment.Left => HorizontalAlignment.Left,
            TextAlignment.Center => HorizontalAlignment.Center,
            TextAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };

        _grid = new()
        {
            Style = isHeader ? config.Themes.TableHeaderCellGridStyle : config.Themes.TableCellGridStyle,
        };
        _grid.Children.Add(_flowDocument.StackPanel);
        _container.UIElement = _grid;
    }

    public void AddChild(IAddChild child)
    {
        _flowDocument.AddChild(child);
    }
}
