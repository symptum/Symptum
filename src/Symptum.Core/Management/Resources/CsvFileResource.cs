using System.Text.Json.Serialization;

namespace Symptum.Core.Management.Resources;

public abstract class CsvFileResource : FileResource
{

    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Csv;

    protected override void OnReadFileText(string text) => OnReadCSV(text);

    protected override void OnReadFileStream(Stream stream) { }

    protected override string OnWriteFileText() => OnWriteCSV();

    protected override Stream? OnWriteFileStream() => null;

    protected abstract void OnReadCSV(string csv);

    protected abstract string OnWriteCSV();
}
