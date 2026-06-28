using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Controls;

namespace Symptum.Editor.Pages;

public class EditorPagesManager
{
    private static readonly Dictionary<Type, IEditorDialog> _dialogInstances = new()
    {
        { typeof(ResourcePropertiesEditorDialog), new ResourcePropertiesEditorDialog() },
    };

    private static readonly Dictionary<Type, Type> _editorTypeMap = new()
    {
        { typeof(ReferenceValueGroup), typeof(ReferenceValueGroupEditorPage) },
        { typeof(MarkdownFileResource), typeof(MarkdownEditorPage) },
        { typeof(ImageFileResource), typeof(ImageViewerPage) }
    };

    public static ObservableCollection<IEditorPage> EditorPages { get; private set; } = [];

    public static EventHandler<IEditorPage?> SelectEditorRequested;

    public static void ShowWelcomePage()
    {
        IEditorPage? welcomePage = EditorPages.FirstOrDefault(x => x is WelcomePage);
        if (welcomePage == null)
        {
            welcomePage = new WelcomePage
            {
                EditableContent = new CategoryResource() { Title = "Welcome" }
            };
            EditorPages.Add(welcomePage);
        }

        SelectEditorRequested?.Invoke(null, welcomePage);
    }
    
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

        SelectEditorRequested?.Invoke(null, editor);
    }

    public static bool TryCloseEditor(IEditorPage? editor)
    {
        if (editor != null && EditorPages.Contains(editor))
        {
            editor.EditableContent = null;
            editor.Dispose();
            EditorPages.Remove(editor);
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
            editor.Dispose();
        }
        EditorPages.Clear();
    }

    public static void CloseSavedEditors()
    {
        var savedEditors = EditorPages.Where(e => !e.HasUnsavedChanges);
        foreach (var e in savedEditors)
        {
            EditorPages.Remove(e);
        }
    }

    public static void UpdateEditors()
    {
        foreach (var editor in EditorPages)
        {
            editor.UpdateContent();
        }
    }

    public static T? CreateOrGetDialog<T>() where T : class, IEditorDialog
    {
        if (_dialogInstances.TryGetValue(typeof(T), out IEditorDialog? dialogInstance))
        {
            return dialogInstance as T;
        }
        else
        {
            dialogInstance = Activator.CreateInstance(typeof(T)) as IEditorDialog;
            if (dialogInstance != null)
            {
                _dialogInstances[typeof(T)] = dialogInstance;
                return dialogInstance as T;
            }
        }

        return null;
    }
}
