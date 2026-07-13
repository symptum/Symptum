using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class ParagraphElement : IAddChild
{
    private ParagraphBlock _paragraphBlock;
    private SParagraph _paragraph;
    private MarkdownConfiguration _config;

    public STextElement TextElement => _paragraph;

    public ParagraphElement(ParagraphBlock paragraphBlock, MarkdownConfiguration config)
    {
        _paragraphBlock = paragraphBlock;
        _config = config;
        _paragraph = new()
        {
            TextBlockStyle = config.Themes.BodyTextBlockStyle,
            IsTextSelectionEnabled = config.IsTextSelectionEnabled
        };
    }

    public void AddChild(IAddChild child)
    {
        if (child is ICascadeChild cascadeChild)
            cascadeChild.InheritProperties(this);
        _paragraph.AddInline(child.TextElement);
    }
}
