using System.Text.Json.Serialization;
using Symptum.Core.Data;
using Symptum.Core.Management.Navigation;

namespace Symptum.Core.Management.Resources;

public abstract class FileResource : NavigableResource, IContent
{
    #region Properties

    #region IContent

    [JsonIgnore]
    public abstract ContentFileType FileType { get; }

    private string? description;

    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    private IList<AuthorInfo>? authors;

    public IList<AuthorInfo>? Authors
    {
        get => authors;
        set => SetProperty(ref authors, value);
    }

    private DateOnly? dateModified;

    public DateOnly? DateModified
    {
        get => dateModified;
        set => SetProperty(ref dateModified, value);
    }

    private IList<string>? tags;

    public IList<string>? Tags
    {
        get => tags;
        set => SetProperty(ref tags, value);
    }

    private IList<string>? seeAlso;

    public IList<string>? SeeAlso
    {
        get => seeAlso;
        set => SetProperty(ref seeAlso, value);
    }

    #endregion

    private string? filePath;

    public string? FilePath
    {
        get => filePath;
        set => SetProperty(ref filePath, value);
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
    }

    internal void ReadFileText(string content)
    {
        OnReadFileText(content);
    }

    internal void ReadFileStream(Stream stream)
    {
        OnReadFileStream(stream);
    }

    internal string WriteFileText()
    {
        return OnWriteFileText();
    }

    internal Stream? WriteFileStream()
    {
        return null;
    }

    protected abstract void OnReadFileText(string content);

    protected abstract void OnReadFileStream(Stream stream);

    protected abstract string OnWriteFileText();

    protected abstract Stream? OnWriteFileStream();

    #region Ignore

    // Since the instances of FileResource are always the end resources (i.e. no children),
    // they shouldn't have any implementations for ChildResources

    [JsonIgnore]
    public override bool CanHandleChildren => false;

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return false;
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return false;
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        throw new NotImplementedException();
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        throw new NotImplementedException();
    }

    #endregion
}
