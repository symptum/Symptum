using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Management.Resources;

public sealed partial class MarkdownFileResource : FileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Markdown;

    [JsonIgnore]
    [ObservableProperty]
    public partial string Markdown { get; set; }

    protected override void OnReadFileText(string content) => Markdown = content;

    protected override string OnWriteFileText() => Markdown;
}
