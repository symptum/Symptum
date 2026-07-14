using HtmlAgilityPack;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class HeadingElement : IAddChild
{
    private SParagraph _paragraph = new();
    private HeadingBlock? _headingBlock;
    private HtmlNode? _htmlNode;
    private MarkdownTextBlock _control;

    public STextElement TextElement => _paragraph;

    public HeadingElement(HeadingBlock headingBlock, MarkdownTextBlock control, DocumentOutline outline)
    {
        _headingBlock = headingBlock;
        LoadHeadingElement(control, outline, headingBlock.Level,
            headingBlock.GetAttributes().Id, headingBlock.Inline?.FirstChild?.ToString());
    }

    public HeadingElement(HtmlNode htmlNode, MarkdownTextBlock control, DocumentOutline outline)
    {
        _htmlNode = htmlNode;
        var align = _htmlNode.GetAttribute("align", "left");
        _paragraph.TextAlignment = align switch
        {
            "left" => TextAlignment.Left,
            "right" => TextAlignment.Right,
            "center" => TextAlignment.Center,
            "justify" => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };

        if (int.TryParse(htmlNode.Name.AsSpan(1), out int level))
            LoadHeadingElement(control, outline, level, htmlNode.Id, htmlNode.InnerText);
    }

    private void LoadHeadingElement(MarkdownTextBlock control, DocumentOutline outline, int level, string? id, string? title)
    {
        _control = control;
        _paragraph.TextBlockStyle = level switch
        {
            1 => _control.H1TextBlockStyle,
            2 => _control.H2TextBlockStyle,
            3 => _control.H3TextBlockStyle,
            4 => _control.H4TextBlockStyle,
            5 => _control.H5TextBlockStyle,
            _ => _control.H6TextBlockStyle,
        };

        DocumentNode node = new()
        {
            Id = id,
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
            Title = title
        };

        outline.PushNode(node);
    }

    public void OnNavigate()
    {
        _paragraph.UIElement?.StartBringIntoView(new() { HorizontalAlignmentRatio = 0, VerticalAlignmentRatio = 0 });
    }

    public void AddChild(IAddChild child)
    {
        if (child is ICascadeChild cascadeChild)
            cascadeChild.InheritProperties(this);
        _paragraph.AddInline(child.TextElement);
    }
}
