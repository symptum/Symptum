using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;

namespace Symptum.Markdown.Mermaid;

public class MermaidBlockExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        if (pipeline.BlockParsers.Contains<MermaidFencedBlockParser>())
        {
            return;
        }

        if (!pipeline.BlockParsers.InsertBefore<FencedCodeBlockParser>(new MermaidFencedBlockParser()))
        {
            pipeline.BlockParsers.Insert(0, new MermaidFencedBlockParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}
