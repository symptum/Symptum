using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Controls;
using Symptum.Editor.ViewModels;

namespace Symptum.Editor.Pages;

public class EditorPagesManager
{
    private static readonly Dictionary<Type, IEditorDialog> _dialogInstances = [];

    private static readonly Dictionary<Type, Type> _editorTypeMap = new()
    {
        { typeof(ReferenceValueGroup), typeof(ReferenceValueGroupEditorPage) },
        { typeof(MarkdownFileResource), typeof(MarkdownEditorPage) },
        { typeof(ImageFileResource), typeof(ImageViewerPage) },
        { typeof(AudioFileResource), typeof(AudioEditorPage) }
    };

    public static ObservableCollection<EditorPageBase> EditorPages { get; private set; } = [];
    private static readonly Dictionary<IResource, EditorPageBase> _resourceToEditorMap = [];
    private static WelcomePage? _welcomePage;

    public static EventHandler<EditorPageBase?> SelectEditorRequested;

    public static void ShowWelcomePage()
    {
        if (_welcomePage == null)
        {
            _welcomePage = new WelcomePage();
            EditorPages.Add(_welcomePage);
        }

        SelectEditorRequested?.Invoke(null, _welcomePage);
    }

    public static EditorPageBase? GetEditorForContentType(Type contentType)
    {
        if (_editorTypeMap.TryGetValue(contentType, out Type? pageType))
        {
            return (pageType != null) ? Activator.CreateInstance(pageType) as EditorPageBase : null;
        }
        else if (typeof(IResource).IsAssignableFrom(contentType))
            return Activator.CreateInstance<DefaultEditorPage>();
        return null;
    }

    public static void CreateOrOpenEditor(IResource? resource)
    {
        if (resource == null) return;

        if (!_resourceToEditorMap.TryGetValue(resource, out var editor))
        {
            editor = GetEditorForContentType(resource.GetType());
            if (editor != null)
            {
                editor.EditableContent = resource;
                EditorPages.Add(editor);
                _resourceToEditorMap[resource] = editor;
            }
        }

        SelectEditorRequested?.Invoke(null, editor);
    }

    public static bool TryCloseEditor(EditorPageBase? editor)
    {
        if (editor != null && EditorPages.Contains(editor))
        {
            if (editor.EditableContent != null)
                _resourceToEditorMap.Remove(editor.EditableContent);
            editor.EditableContent = null;
            editor.Dispose();
            EditorPages.Remove(editor);
            if (editor == _welcomePage) _welcomePage = null;

            if (MainViewModel.Instance.CurrentEditor == editor)
                MainViewModel.Instance.CurrentEditor = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }

        if (EditorPages.Count == 0)
        {
            _dialogInstances.Clear();
        }

        return false;
    }

    public static bool TryCloseEditorForResource(IResource? resource) =>
        resource != null && _resourceToEditorMap.TryGetValue(resource, out var editor) && TryCloseEditor(editor);


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
        _resourceToEditorMap.Clear();
        _dialogInstances.Clear();
        _welcomePage = null;
        MainViewModel.Instance.CurrentEditor = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public static void CloseSavedEditors()
    {
        List<EditorPageBase> savedEditors = [.. EditorPages.Where(e => !e.HasUnsavedChanges)];
        foreach (var e in savedEditors)
        {
            if (e.EditableContent != null)
                _resourceToEditorMap.Remove(e.EditableContent);
            e.EditableContent = null;
            e.Dispose();
            EditorPages.Remove(e);
            if (e == _welcomePage) _welcomePage = null;
            if (MainViewModel.Instance.CurrentEditor == e)
                MainViewModel.Instance.CurrentEditor = null;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
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
