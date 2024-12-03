using System.Text.Json.Serialization;

namespace Symptum.Core.Management.Resources;

public class ImageFileResource : FileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Image;

    protected override void OnReadFileStream(Stream stream) => throw new NotImplementedException();

    protected override void OnReadFileText(string content) => throw new NotImplementedException();

    protected override Stream? OnWriteFileStream() => throw new NotImplementedException();

    protected override string OnWriteFileText() => throw new NotImplementedException();
}
