using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Pages;

public interface IEditorPage
{
    public IconSource? IconSource { get; }

    public IResource? EditableContent { get; set; }

    public bool HasUnsavedChanges { get; set; }

    public void UpdateContent();
}
