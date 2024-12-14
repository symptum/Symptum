using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Data;
using Symptum.Core.Management.Navigation;

namespace Symptum.Core.Management.Resources;

public abstract partial class FileResource : NavigableResource, IContent
{
    #region Properties

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial string? FileExtension { get; protected set; }

    [JsonIgnore]
    public abstract ContentFileType FileType { get; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial IList<AuthorInfo>? Authors { get; set; }

    [ObservableProperty]
    public partial DateOnly? DateModified { get; set; }

    [ObservableProperty]
    public partial IList<string>? Tags { get; set; }

    [ObservableProperty]
    public partial IList<string>? SeeAlso { get; set; }

    #endregion

    protected override void OnInitializeResource(IResource? parent) { }

    #region Ignore

    // Since the instances of FileResource are always the end resources (i.e. no children),
    // they shouldn't have any implementations for ChildResources

    [JsonIgnore]
    public override bool CanHandleChildren => false;

    public override bool CanHandleChildResourceType(Type childResourceType) => false;

    public override bool CanAddChildResourceType(Type childResourceType) => false;

    protected override void OnAddChildResource(IResource? childResource) => throw new NotImplementedException();

    protected override void OnRemoveChildResource(IResource? childResource) => throw new NotImplementedException();

    #endregion
}

public abstract class TextFileResource : FileResource
{
    internal void ReadFileText(string content) => OnReadFileText(content);

    internal string? WriteFileText() => OnWriteFileText();

    protected abstract void OnReadFileText(string content);

    protected abstract string? OnWriteFileText();
}

public abstract class MediaFileResource : FileResource
{
    public void SetMediaFileExtension(string? extension)
    {
        FileExtension = extension;
    }
}

public sealed partial class ImageFileResource : MediaFileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Image;
}

public sealed partial class AudioFileResource : MediaFileResource
{
    [JsonIgnore]
    public override ContentFileType FileType { get; } = ContentFileType.Audio;
}
