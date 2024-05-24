using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Navigation;

namespace Symptum.Core.Management.Resources;

public abstract class NavigableResource : ObservableObject, IResource, INavigable
{
    #region Properties

    #region IResource

    private Uri? uri;

    public Uri? Uri
    {
        get => uri;
        set => SetProperty(ref uri, value);
    }

    private string? id = string.Empty;

    public string? Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    private string? _title = string.Empty;

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private IResource? parentResource;

    [JsonIgnore]
    public IResource? ParentResource
    {
        get => parentResource;
        private set => SetProperty(ref parentResource, value);
    }

    private ObservableCollection<IResource>? childrenResources;

    [JsonIgnore]
    public IReadOnlyList<IResource>? ChildrenResources
    {
        get => childrenResources;
    }

    private IList<IResource>? dependencies;

    [JsonIgnore]
    public IList<IResource>? Dependencies
    {
        get => dependencies;
        set => SetProperty(ref dependencies, value);
    }

    private IList<string>? dependencyIds;

    [JsonPropertyName(nameof(Dependencies))]
    public IList<string>? DependencyIds
    {
        get => dependencyIds;
        set => SetProperty(ref dependencyIds, value);
    }

    #endregion

    private bool hasInitialized = false;

    [JsonIgnore]
    public bool HasInitialized
    {
        get => hasInitialized;
        private set => SetProperty(ref hasInitialized, value);
    }

    #endregion

    void IResource.InitializeResource(IResource? parent)
    {
        ParentResource = parent;
        SetProperty(ref childrenResources, [], nameof(ChildrenResources));
        OnInitializeResource(parent);

        // Temporary
        if (childrenResources != null)
        {
            foreach (var child in childrenResources)
            {
                child.InitializeResource(this);
            }
        }
        hasInitialized = true;
    }

    protected abstract void OnInitializeResource(IResource? parent);

    public abstract bool CanHandleChildResourceType(Type childResourceType);

    public abstract bool CanAddChildResourceType(Type childResourceType);

    public void AddChildResource(IResource? childResource)
    {
        OnAddChildResource(childResource);
        if (hasInitialized)
            childResource?.InitializeResource(this); // Temporary
    }

    public void RemoveChildResource(IResource? childResource)
    {
        OnRemoveChildResource(childResource);
    }

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

    protected void AddChildResourceInternal(IResource? childResource)
    {
        if (childResource != null)
            childrenResources?.AddItemToListIfNotExists(childResource);
    }

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

    protected void RemoveChildResourceInternal(IResource? childResource)
    {
        childrenResources?.RemoveItemFromListIfExists(childResource);
    }

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

    protected void UnobserveCollection<T>(ObservableCollection<T>? collection) where T : IResource
    {
        if (hasInitialized)
            childrenResources?.Clear();
        if (collection != null)
            collection.CollectionChanged -= Collection_Changed;
    }

    protected void ObserveCollection<T>(ObservableCollection<T> collection) where T : IResource
    {
        if (collection == null) return;
        collection.CollectionChanged += Collection_Changed;
    }

    private void Collection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!hasInitialized || childrenResources == null) return;
        //bool isCR = sender == childrenResources;

        //if (!isCR)
        //{
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
        //}
        //else
        //    HandleChildrenChanged(e);
    }

    //private void HandleChildrenChanged(NotifyCollectionChangedEventArgs e)
    //{
    //    switch (e.Action)
    //    {
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
    //            {
    //                OnChildrenReset();
    //                break;
    //            }
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
    //            {
    //                OnChildrenAdded(e.NewItems?.OfType<IResource>());
    //                break;
    //            }
    //        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
    //            {
    //                OnChildrenRemoved(e.OldItems?.OfType<IResource>());
    //                break;
    //            }
    //    }
    //}
}
