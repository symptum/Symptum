namespace Symptum.UI.Markdown.TextElements;

public class FlowDocumentElement : IAddChild
{
    private StackPanel _stackPanel = new();
    private SContainer _container = new();
    private MarkdownTextBlock _control;

    public STextElement TextElement => _container;

    public StackPanel StackPanel
    {
        get => _stackPanel;
        set => _stackPanel = value;
    }

    public Style? TextBlockStyle { get; set; }

    public FlowDocumentElement(MarkdownTextBlock control, bool isTopLevel = true)
    {
        _stackPanel.Style = control.FlowDocumentStackPanelStyle;
        if (!isTopLevel) _stackPanel.Padding = new();
        _control = control;

        _container.UIElement = _stackPanel;
    }

    public void AddChild(IAddChild child)
    {
        if (child is ICascadeChild cascadeChild)
            cascadeChild.InheritProperties(this);

        STextElement element = child.TextElement;
        if (element != null)
        {
            if (element is SInline inline)
            {
                TextBlock _textBlock = new()
                {
                    Style = TextBlockStyle ?? _control.BodyTextBlockStyle,
                    IsTextSelectionEnabled = _control.IsTextSelectionEnabled
                };
                _textBlock.Inlines.Add(inline.Inline);
                _stackPanel.Children.Add(_textBlock);
            }
            else if (element is SBlock block)
            {
                if (block is SParagraph paragraph)
                {
                    if (TextBlockStyle != null) paragraph.TextBlockStyle = TextBlockStyle;
                    paragraph.IsTextSelectionEnabled = _control.IsTextSelectionEnabled;
                    paragraph.CreateUIElement();
                }

                if (block.UIElement is UIElement ui)
                    _stackPanel.Children.Add(ui);
            }
        }
    }
}
