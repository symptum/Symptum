using Symptum.Markdown.Mermaid;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers;

public class MermaidBlockRenderer : WinUIObjectRenderer<MermaidDiagramBlock>
{
    protected override void Write(WinUIRenderer renderer, MermaidDiagramBlock obj)
    {
        MermaidBlockElement element = new(obj, renderer.MarkdownTextBlock);
        renderer.Push(element);
        renderer.Pop();
    }
}
