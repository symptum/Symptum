namespace Symptum.Editor.Controls;

public class ListEditorItemWrapper<T>
{
    public T Value { get; set; }

    public ListEditorItemWrapper()
    { }

    public ListEditorItemWrapper(T value)
    {
        Value = value;
    }
}

public static class ListOfListEditorItemWrapperExtensions
{
    public static void LoadFromList<T>(this IList<ListEditorItemWrapper<T>> destination, IList<T>? source)
    {
        if (destination == null) return;

        destination.Clear();

        if (source == null || source.Count == 0) return;

        foreach (var item in source)
        {
            destination.Add(new ListEditorItemWrapper<T>(item));
        }
    }

    public static List<T> UnwrapToList<T>(this IList<ListEditorItemWrapper<T>> source)
    {
        List<T> list = [];
        if (source == null || source.Count == 0) return list;

        foreach (var item in source)
        {
            list.Add(item.Value);
        }

        return list;
    }
}

