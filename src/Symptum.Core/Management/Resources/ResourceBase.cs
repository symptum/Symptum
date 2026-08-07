using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Extensions;

namespace Symptum.Core.Management.Resources;

// This is the base implementation of IResource, it doesn't implement INavigable.
// This class contains all the properties, logic for loading, adding and removing children resources.
public abstract partial class ResourceBase : ObservableObject, IResource
{
    #region Properties

    #region IResource

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Id { get; set; }

    [ObservableProperty]
    public partial Uri? Uri {  get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial IResource? ParentResource {  get; private set; }

    private ObservableCollection<IResource>? childrenResources;

    [JsonIgnore]
    public IReadOnlyList<IResource>? ChildrenResources
    {
        get => childrenResources;
    }

    [JsonIgnore]
    [ObservableProperty]
    public partial IList<IResource>? Dependencies { get; set; }

    [JsonPropertyName(nameof(Dependencies))]
    [ObservableProperty]
    public partial IList<string>? DependencyIds { get; set; }

    [JsonIgnore]
    public virtual bool CanHandleChildren { get; } = true;

    #endregion

    [JsonIgnore]
    [ObservableProperty]
    public partial bool HasInitialized { get; private set; }

    #endregion

    void IResource.InitializeResource(IResource? parent)
    {
        if (HasInitialized) return;
        
        ParentResource = parent;
        SetProperty(ref childrenResources, CanHandleChildren ? [] : null, nameof(ChildrenResources));
        OnInitializeResource(parent);

        HasInitialized = true;
    }

    protected abstract void OnInitializeResource(IResource? parent);

    public abstract bool CanHandleChildResourceType(Type childResourceType);

    public abstract bool CanAddChildResourceType(Type childResourceType);

    public void AddChildResource(IResource? childResource)
    {
        OnAddChildResource(childResource);
        if (HasInitialized)
            childResource?.InitializeResource(this); // Temporary
    }

    public void RemoveChildResource(IResource? childResource) => OnRemoveChildResource(childResource);

    protected abstract void OnAddChildResource(IResource? childResource);

    protected abstract void OnRemoveChildResource(IResource? childResource);

    protected void AddChildrenResourcesInternal(IList? children)
    {
        if (children?.Count > 0)
        {
            foreach (var child in children)
            {
                AddChildResourceInternal(child as IResource);
            }
        }
    }

    protected void AddChildResourceInternal(IResource? childResource) => childrenResources?.AddItemToListIfNotExists(childResource);

    protected void RemoveChildrenResourcesInternal(IList? children)
    {
        if (children?.Count > 0)
        {
            foreach (var child in children)
            {
                RemoveChildResourceInternal(child as IResource);
            }
        }
    }

    protected void RemoveChildResourceInternal(IResource? childResource) => childrenResources?.RemoveItemFromListIfExists(childResource);

    protected void SetChildrenResources<T>(ObservableCollection<T>? collection) where T : IResource
    {
        if (collection != null && childrenResources != null)
        {
            foreach (var item in collection)
            {
                childrenResources.Add(item);
            }
            ObserveCollection(collection);
        }
    }

    bool _isObservingCollection = false;

    protected void UnobserveCollection<T>(ObservableCollection<T>? collection) where T : IResource
    {
        if (HasInitialized)
            childrenResources?.Clear();
        if (collection != null)
        {
            collection.CollectionChanged -= Collection_Changed;
            _isObservingCollection = false;
        }
    }

    protected void ObserveCollection<T>(ObservableCollection<T> collection) where T : IResource
    {
        if (collection == null || _isObservingCollection) return;
        collection.CollectionChanged += Collection_Changed;
        _isObservingCollection = true;
    }

    private void Collection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!HasInitialized || childrenResources == null) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                {
                    childrenResources.Clear();
                    break;
                }
            case NotifyCollectionChangedAction.Add:
                {
                    AddChildrenResourcesInternal(e.NewItems);
                    break;
                }
            case NotifyCollectionChangedAction.Remove:
                {
                    RemoveChildrenResourcesInternal(e.OldItems);
                    break;
                }
        }
    }
}
