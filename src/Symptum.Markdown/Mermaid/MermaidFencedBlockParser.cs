using Markdig.Parsers;

namespace Symptum.Markdown.Mermaid;

public class MermaidFencedBlockParser : FencedBlockParserBase<MermaidDiagramBlock>
{
    public MermaidFencedBlockParser()
    {
        OpeningCharacters = ['`', '~'];
        InfoPrefix = null;
    }

    protected override MermaidDiagramBlock CreateFencedBlock(BlockProcessor processor)
    {
        return new MermaidDiagramBlock(this);
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        var result = base.TryOpen(processor);
        if (result == BlockState.None)
        {
            return result;
        }

        if (processor.NewBlocks.Count == 0 || processor.NewBlocks.Peek() is not MermaidDiagramBlock mermaidBlock)
        {
            return BlockState.None;
        }

        if (mermaidBlock.TryInitializeDescriptor())
        {
            return result;
        }

        processor.NewBlocks.Pop();
        return BlockState.None;
    }
}
