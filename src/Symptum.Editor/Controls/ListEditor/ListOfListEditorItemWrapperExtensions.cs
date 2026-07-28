using System.Collections.ObjectModel;

namespace Symptum.Editor.Controls;

public static class ListOfListEditorItemWrapperExtensions
{
    public static void LoadFromList<T>(this IList<ListEditorItemWrapper<T>> destination, IList<T>? source)
    {
        if (destination == null) return;

        destination.ClearWrapperListSafe();

        if (source == null || source.Count == 0) return;

        foreach (var item in source)
        {
            destination.Add(new(item));
        }
    }

    public static List<T>? UnwrapToList<T>(this IList<ListEditorItemWrapper<T>> source)
    {
        if (source == null || source.Count == 0) return null;

        List<T> list = [];
        foreach (var item in source)
        {
            list.Add(item.Value);
        }

        return list;
    }

    public static void RemoveWrapperSafe<T>(this IList<ListEditorItemWrapper<T>> source,
        ListEditorItemWrapper<T>? wrapper)
    {
        if (source == null || source.Count == 0 ||
            wrapper == null || wrapper.Value == null) return;

        // Remove the value reference before removing the wrapper from the list. 
        wrapper.Value = default;
        source.Remove(wrapper);
    }

    public static void ClearWrapperListSafe<T>(this IList<ListEditorItemWrapper<T>> source)
    {
        if (source == null || source.Count == 0) return;

        foreach (var wrapper in source)
            wrapper.Value = default;
        source.Clear();
    }

    public static void MoveWrapperUp<T>(this ObservableCollection<ListEditorItemWrapper<T>> source,
        ListEditorItemWrapper<T>? wrapper)
    {
        if (source == null || source.Count == 0 || wrapper == null) return;

        int oldIndex = source.IndexOf(wrapper);
        int newIndex = Math.Max(oldIndex - 1, 0);
        source.Move(oldIndex, newIndex);
    }

    public static void MoveWrapperDown<T>(this ObservableCollection<ListEditorItemWrapper<T>> source,
        ListEditorItemWrapper<T>? wrapper)
    {
        if (source == null || source.Count == 0 || wrapper == null) return;

        int oldIndex = source.IndexOf(wrapper);
        int newIndex = Math.Min(oldIndex + 1, source.Count - 1);
        source.Move(oldIndex, newIndex);
    }
}

