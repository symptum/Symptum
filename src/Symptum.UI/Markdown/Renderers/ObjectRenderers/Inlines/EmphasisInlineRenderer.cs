using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

public class EmphasisInlineRenderer : WinUIObjectRenderer<EmphasisInline>
{
    protected override void Write(WinUIRenderer renderer, EmphasisInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        EmphasisInlineElement? span = null;

        var control = renderer.MarkdownTextBlock;

        switch (obj.DelimiterChar)
        {
            case '*':
            case '_':
                span = new EmphasisInlineElement(obj, control);
                if (obj.DelimiterCount == 2) { span.SetBold(); } else { span.SetItalic(); }
                break;
            case '~':
                span = new EmphasisInlineElement(obj, control);
                if (obj.DelimiterCount == 2) { span.SetStrikeThrough(); } else { span.SetSubscript(); }
                break;
            case '^':
                span = new EmphasisInlineElement(obj, control);
                span.SetSuperscript();
                break;
        }

        if (span != null)
        {
            renderer.Push(span);
            renderer.WriteChildren(obj);
            renderer.Pop();
        }
        else
        {
            renderer.WriteChildren(obj);
        }
    }
}
