using Symptum.Core.Management.Resources;

namespace Symptum.Editor.EditorPages;

public interface IEditorPage
{
    public string Title { get; set; }

    public IconSource IconSource { get; }

    public IResource? EditableContent { get; set; }

    public bool HasUnsavedChanges { get; set; }
}