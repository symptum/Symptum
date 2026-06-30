using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Pages;

public interface IEditorPage : IDisposable
{
    public IconSource? IconSource { get; }

    public string? Title { get; set; }

    public IResource? EditableContent { get; set; }

    public bool HasUnsavedChanges { get; set; }

    public void UpdateContent();
}
