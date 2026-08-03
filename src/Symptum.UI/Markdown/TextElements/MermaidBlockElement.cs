using Symptum.Markdown.Mermaid;
using Symptum.UI.Markdown.Mermaid;

namespace Symptum.UI.Markdown.TextElements;

public class MermaidBlockElement : IAddChild
{
    private readonly SContainer _container = new();

    public STextElement TextElement => _container;

    public MermaidBlockElement(MermaidDiagramBlock block, MarkdownTextBlock control)
    {
        string source = MermaidSyntax.NormalizeCode(block.Lines.ToString());
        MermaidDiagramDefinition definition = MermaidDiagramParser.Parse(source);
        _container.UIElement = MermaidDiagramViewFactory.Create(definition, control.RequestedTheme);
    }

    public void AddChild(IAddChild child) { }
}
