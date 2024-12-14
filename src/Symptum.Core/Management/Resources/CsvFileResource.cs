using System.Text.Json.Serialization;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Core.Management.Resources;

public abstract partial class CsvFileResource : TextFileResource
{
    public CsvFileResource()
    {
        FileExtension = CsvFileExtension;
    }

    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Csv;

    protected override void OnReadFileText(string text) => OnReadCSV(text);

    protected override string? OnWriteFileText() => OnWriteCSV();

    protected abstract void OnReadCSV(string csv);

    protected abstract string OnWriteCSV();
}
