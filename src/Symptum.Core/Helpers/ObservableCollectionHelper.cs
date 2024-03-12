using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Helpers;

public static class ObservableCollectionHelper
{
    private static Dictionary<NavigableResource, ObservableCollection<IResource>> collections = [];

    public static void ObserveCollection(this NavigableResource navigableResource, ObservableCollection<IResource> collection)
    {
        if (collection == null) return;
        collection.CollectionChanged += Collection_CollectionChanged;
        collections[navigableResource] = collection;
    }

    private static void Collection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
    }
}
