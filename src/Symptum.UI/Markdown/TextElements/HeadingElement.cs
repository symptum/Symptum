using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class HeadingElement : IAddChild
{
    private SParagraph _paragraph;
    private HeadingBlock? _headingBlock;
    private MarkdownConfiguration _config;

    public STextElement TextElement
    {
        get => _paragraph;
    }

    public HeadingElement(HeadingBlock headingBlock, MarkdownConfiguration config, DocumentOutline outline)
    {
        _headingBlock = headingBlock;
        _paragraph = new();
        _config = config;

        int level = headingBlock.Level;
        _paragraph.TextBlockStyle = level switch
        {
            1 => _config.Themes.H1TextBlockStyle,
            2 => _config.Themes.H2TextBlockStyle,
            3 => _config.Themes.H3TextBlockStyle,
            4 => _config.Themes.H4TextBlockStyle,
            5 => _config.Themes.H5TextBlockStyle,
            _ => _config.Themes.H6TextBlockStyle,
        };

        DocumentNode node = new()
        {
            Id = headingBlock.GetAttributes().Id,
            Level = level switch
            {
                1 => DocumentLevel.Heading1,
                2 => DocumentLevel.Heading2,
                3 => DocumentLevel.Heading3,
                4 => DocumentLevel.Heading4,
                5 => DocumentLevel.Heading5,
                _ => DocumentLevel.Heading6,
            },
            Navigate = OnNavigate,
            Title = headingBlock.Inline?.FirstChild?.ToString()
        };

        outline.PushNode(node);
    }

    public void OnNavigate()
    {
        _paragraph.UIElement?.StartBringIntoView(new() { HorizontalAlignmentRatio = 0, VerticalAlignmentRatio = 0 });
    }

    public void AddChild(IAddChild child)
    {
        _paragraph.AddInline(child.TextElement);
    }
}
