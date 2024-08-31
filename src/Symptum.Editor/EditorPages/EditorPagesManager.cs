using System.Collections.ObjectModel;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Editor.EditorPages;

public class EditorPagesManager
{
    private static readonly Dictionary<Type, Type> _editorTypeMap = new()
    {
        { typeof(QuestionBankTopic), typeof(QuestionTopicEditorPage) },
        { typeof(ReferenceValueGroup), typeof(ReferenceValueGroupEditorPage) },
        { typeof(FoodGroup), typeof(FoodGroupEditorPage) }
    };

    public static ObservableCollection<IEditorPage> EditorPages { get; private set; } = [];

    public static EventHandler<IEditorPage?> CurrentEditorChanged;

    public static IEditorPage? GetEditorForContentType(Type contentType)
    {
        if (_editorTypeMap.TryGetValue(contentType, out Type? pageType))
        {
            return (pageType != null) ? Activator.CreateInstance(pageType) as IEditorPage : null;
        }
        else if (typeof(IResource).IsAssignableFrom(contentType))
            return Activator.CreateInstance(typeof(DefaultEditorPage)) as IEditorPage;
        return null;
    }

    public static void CreateOrOpenEditor(IResource? resource)
    {
        if (resource == null) return;

        IEditorPage? editor = EditorPages.FirstOrDefault(x => x.EditableContent == resource);
        if (editor == null)
        {
            editor = GetEditorForContentType(resource.GetType());
            if (editor != null)
            {
                editor.EditableContent = resource;
                EditorPages.Add(editor);
            }
        }

        CurrentEditorChanged?.Invoke(null, editor);
    }

    public static bool TryCloseEditor(IEditorPage? editor)
    {
        if (editor != null && EditorPages.Contains(editor))
        {
            EditorPages.Remove(editor);
            editor.EditableContent = null;
            return true;
        }

        return false;
    }

    public static void MarkAllOpenEditorsAsSaved()
    {
        foreach (var editor in EditorPages)
        {
            editor.HasUnsavedChanges = false;
        }
    }

    public static void ResetEditors()
    {
        foreach (var editor in EditorPages)
        {
            editor.EditableContent = null;
        }
        EditorPages.Clear();
    }
}
