using System.Collections.ObjectModel;
using Symptum.Core.Extensions;
using Symptum.Core.Serialization;

namespace Symptum.Core.Management.Resources;

public class CategoryResource : CategoryResource<MetadataResource>
{
    [ListOfMetadataResource]
    public override ObservableCollection<MetadataResource>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }
}

public class MarkdownCategoryResource : CategoryResource<MarkdownFileResource> { }

public class ImageCategoryResource : CategoryResource<ImageFileResource> { }

public class CategoryResource<T> : MetadataResource where T : IResource
{
    #region Properties

    private ObservableCollection<T>? items;

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

    protected override void OnInitializeResource(IResource? parent)
    {
        SetChildrenResources(Items);
    }

    public override bool CanHandleChildResourceType(Type childResourceType)
    {
        return typeof(T).IsAssignableFrom(childResourceType);
    }

    public override bool CanAddChildResourceType(Type childResourceType)
    {
        return CanHandleChildResourceType(childResourceType);
    }

    protected override void OnAddChildResource(IResource? childResource)
    {
        Items ??= [];
        Items.AddItemToListIfNotExists(childResource);
    }

    protected override void OnRemoveChildResource(IResource? childResource)
    {
        Items.RemoveItemFromListIfExists(childResource);
    }
}
