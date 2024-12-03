using Symptum.Core.Management.Resources;

namespace Symptum.Editor.EditorPages;

public interface IEditorPage
{
    public IconSource? IconSource { get; }

    public IResource? EditableContent { get; set; }

    public bool HasUnsavedChanges { get; set; }
}
