namespace Symptum.Core.Management.Resources;

public abstract class FileResource : NavigableResource
{
    #region Properties

    private string path = string.Empty;

    public string Path
    {
        get => path;
        set => SetProperty(ref path, value);
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent)
    {
    }

    internal void ReadFileContent(string content)
    {
        OnReadFileContent(content);
    }

    internal string WriteFileContent()
    {
        return OnWriteFileContent();
    }

    protected abstract void OnReadFileContent(string content);

    protected abstract string OnWriteFileContent();

    #region Ignore

    // Since the instances of FileResource are always the end resources (i.e. no children),
    // they shouldn't have any implementations for ChildResources

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return false;
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return false;
    }

    protected override void OnAddChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }

    protected override void OnRemoveChildResource(IResource childResource)
    {
        throw new NotImplementedException();
    }

    #endregion
}
