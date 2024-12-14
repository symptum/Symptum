using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Core.Management.Resources;

public sealed partial class MarkdownFileResource : TextFileResource
{
    public MarkdownFileResource()
    {
        FileExtension = MarkdownFileExtension;
    }

    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Markdown;

    [JsonIgnore]
    [ObservableProperty]
    public partial string? Markdown { get; set; }

    protected override void OnReadFileText(string content) => Markdown = content;

    protected override string? OnWriteFileText() => Markdown;
}
