using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Symptum.Core.Management.Resources;

public sealed partial class ImageFileResource : FileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Image;

    [JsonIgnore]
    [ObservableProperty]
    public partial string ImageType { get; set; }

    protected override void OnReadFileText(string content) => throw new NotImplementedException();

    protected override string OnWriteFileText() => throw new NotImplementedException();
}
