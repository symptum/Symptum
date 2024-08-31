using System.Text.Json.Serialization;

namespace Symptum.Core.Management.Resources;

public abstract class MarkdownFileResource : FileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Markdown;
}
