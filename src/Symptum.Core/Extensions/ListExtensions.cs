namespace Symptum.Core.Extensions;

public static class ListExtensions
{
    public static void AddItemToListIfNotExists<T>(this IList<T>? list, object? item)
    {
        if (item is T _item)
        {
            if (list != null && !list.Contains(_item))
                list.Add(_item);
        }
    }

    public static void RemoveItemFromListIfExists<T>(this IList<T>? list, object? item)
    {
        if (item is T _item)
        {
            if (list != null && list.Count > 0 && list.Contains(_item))
                list.Remove(_item);
        }
    }
}
