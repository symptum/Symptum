using Markdig.Syntax;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

public class ThematicBreakRenderer : WinUIObjectRenderer<ThematicBreakBlock>
{
    protected override void Write(WinUIRenderer renderer, ThematicBreakBlock obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        ThematicBreakElement thematicBreak = new(obj, renderer.MarkdownTextBlock);

        renderer.WriteBlock(thematicBreak);
    }
}
