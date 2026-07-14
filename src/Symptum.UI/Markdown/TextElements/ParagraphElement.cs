using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class ParagraphElement : IAddChild
{
    private ParagraphBlock _paragraphBlock;
    private SParagraph _paragraph;

    public STextElement TextElement => _paragraph;

    public ParagraphElement(ParagraphBlock paragraphBlock, MarkdownTextBlock control)
    {
        _paragraphBlock = paragraphBlock;
        _paragraph = new()
        {
            TextBlockStyle = control.BodyTextBlockStyle,
            IsTextSelectionEnabled = control.IsTextSelectionEnabled
        };
    }

    public void AddChild(IAddChild child)
    {
        if (child is ICascadeChild cascadeChild)
            cascadeChild.InheritProperties(this);
        _paragraph.AddInline(child.TextElement);
    }
}
