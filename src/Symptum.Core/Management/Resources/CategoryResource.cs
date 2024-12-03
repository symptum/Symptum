using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Symptum.Core.Extensions;
using Symptum.Core.Serialization;

namespace Symptum.Core.Management.Resources;

public sealed class CategoryResource : CategoryResource<MetadataResource, ISubjectCategoryResource>, ISubjectCategoryResource
{
    [ListOfMetadataResource]
    public override ObservableCollection<MetadataResource>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }
}

public sealed class MarkdownCategoryResource : CategoryResource<MarkdownFileResource>, ISubjectCategoryResource
{
    [JsonPropertyName("Documents")]
    public override ObservableCollection<MarkdownFileResource>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }
}

public sealed class ImageCategoryResource : CategoryResource<ImageFileResource>, ISubjectCategoryResource
{
    [JsonPropertyName("Images")]
    public override ObservableCollection<ImageFileResource>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }
}

public interface ISubjectCategoryResource { }

public class CategoryResource<T> : MetadataResource where T : IResource
{
    #region Properties

    private ObservableCollection<T>? items;

    [JsonIgnore]
    public virtual ObservableCollection<T>? Items
    {
        get => items;
        set
        {
            UnobserveCollection(items);
            SetProperty(ref items, value);
            SetChildrenResources(items);
        }
    }

    #endregion

    protected override void OnInitializeResource(IResource? parent) => SetChildrenResources(Items);

    protected virtual bool ChildRestraint(Type childResourceType) => true;

    public override bool CanHandleChildResourceType(Type childResourceType) =>
        typeof(T).IsAssignableFrom(childResourceType) && ChildRestraint(childResourceType);

    public override bool CanAddChildResourceType(Type childResourceType) => CanHandleChildResourceType(childResourceType);

    protected override void OnAddChildResource(IResource? childResource)
    {
        Items ??= [];
        Items.AddItemToListIfNotExists(childResource);
    }

    protected override void OnRemoveChildResource(IResource? childResource) => Items.RemoveItemFromListIfExists(childResource);
}

public class CategoryResource<T, TCondition> : CategoryResource<T> where T : IResource
{
    protected override bool ChildRestraint(Type childResourceType) => typeof(TCondition).IsAssignableFrom(childResourceType);
}
