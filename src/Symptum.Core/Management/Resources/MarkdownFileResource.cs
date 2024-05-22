namespace Symptum.Core.Management.Resources;

public abstract class MarkdownFileResource : FileResource
{
    public override ContentFileType FileType { get; } = ContentFileType.Markdown;
}
