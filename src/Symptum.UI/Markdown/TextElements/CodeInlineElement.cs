using Markdig.Syntax.Inlines;

namespace Symptum.UI.Markdown.TextElements;

public class CodeInlineElement : IAddChild
{
    private CodeInline _codeInline;
    private SContainer _container = new();

    public STextElement TextElement => _container;

    public CodeInlineElement(CodeInline codeInline, MarkdownTextBlock control)
    {
        _codeInline = codeInline;
        Border border = new()
        {
            Style = control.CodeInlineBorderStyle,
        };
        TextBlock textBlock = new()
        {
            Text = codeInline.Content,
            Style = control.CodeTextBlockStyle,
        };
        border.Child = textBlock;
        _container.UIElement = border;
    }

    public void AddChild(IAddChild child) { }
}
