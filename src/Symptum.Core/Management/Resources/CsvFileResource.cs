namespace Symptum.Core.Management.Resources;

public abstract class CsvFileResource : FileResource
{
    protected override void OnReadFileContent(string content)
    {
        OnReadCSV(content);
    }

    protected override string OnWriteFileContent()
    {
        return OnWriteCSV();
    }

    protected abstract void OnReadCSV(string csv);

    protected abstract string OnWriteCSV();

    public override ContentFileType FileType { get; } = ContentFileType.Csv;
}
