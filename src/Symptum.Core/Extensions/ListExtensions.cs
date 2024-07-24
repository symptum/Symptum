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

    public static List<T> CloneList<T>(this IList<T>? values, Func<T, T>? function = null)
    {
        List<T> results = [];
        if (values != null)
        {
            foreach (T item in values)
            {
                T result = function != null ? function(item) : item;
                results.Add(result);
            }
        }

        return results;
    }
}
