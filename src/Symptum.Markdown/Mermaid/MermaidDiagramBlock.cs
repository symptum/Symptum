using Markdig.Parsers;
using Markdig.Syntax;

namespace Symptum.Markdown.Mermaid;

public class MermaidDiagramBlock : FencedCodeBlock
{
    public MermaidDiagramBlock(BlockParser parser)
        : base(parser)
    {
    }

    public string NormalizedInfo { get; private set; } = "mermaid";

    public string DiagramArguments { get; private set; } = string.Empty;

    public bool TryInitializeDescriptor()
    {
        if (!MermaidSyntax.TryParseDescriptor(Info, Arguments, out var normalizedInfo, out var normalizedArguments))
        {
            return false;
        }

        NormalizedInfo = normalizedInfo;
        DiagramArguments = normalizedArguments;
        return true;
    }
}
