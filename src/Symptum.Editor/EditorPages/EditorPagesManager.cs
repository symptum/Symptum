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

    public static IEditorPage? GetEditorPageForContentType(Type contentType)
    {
        if (_editorTypeMap.TryGetValue(contentType, out Type? pageType))
        {
            return (pageType != null) ? (IEditorPage?)Activator.CreateInstance(pageType) : null;
        }
        return null;
    }

    public static IEditorPage? CreateOrOpenEditorPage(IResource content)
    {
        IEditorPage? page = EditorPages.FirstOrDefault(x => x.EditableContent == content);
        if (page == null)
        {
            page = GetEditorPageForContentType(content.GetType());
            if (page != null)
            {
                page.EditableContent = content;
                EditorPages.Add(page);
            }
        }
        return page;
    }

    public static bool TryCloseEditorPage(IEditorPage? page)
    {
        if (page != null && EditorPages.Contains(page))
        {
            EditorPages.Remove(page);
            page.EditableContent = null;
            return true;
        }

        return false;
    }

    public static void MarkAllOpenEditorsAsSaved()
    {
        foreach (var page in EditorPages)
        {
            page.HasUnsavedChanges = false;
        }
    }

    public static void ResetEditors()
    {
        foreach (var page in EditorPages)
        {
            page.EditableContent = null;
        }
        EditorPages.Clear();
    }
}
