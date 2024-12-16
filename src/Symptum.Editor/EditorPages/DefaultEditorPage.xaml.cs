using Symptum.Common.ProjectSystem;
using Symptum.Editor.Common;

namespace Symptum.Editor.EditorPages;

public sealed partial class DefaultEditorPage : EditorPageBase
{
    public DefaultEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.PropertiesIconSource;
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (EditableContent != null)
        {
            propertiesEditor.UpdateResource();
            HasUnsavedChanges = !await ProjectSystemManager.SaveResourceAndAncestorAsync(EditableContent);
        }
        _isBeingSaved = false;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) => propertiesEditor.ResetResource();
}
