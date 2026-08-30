using Symptum.Common.ProjectSystem;
using Symptum.UI;

namespace Symptum.Editor.Pages;

public sealed partial class DefaultEditorPage : EditorPageBase
{
    public DefaultEditorPage()
    {
        InitializeComponent();
        IconSource = IconHelper.PropertiesIconSource;
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (EditableContent != null)
        {
            propertiesEditor.UpdateResource();
            bool saved = await ProjectSystemManager.SaveResourceAndAncestorAsync(EditableContent);
            HasUnsavedChanges = !saved;
            if (saved)
                WriteToOutput($"Saved: {EditableContent.Title}");
        }
        _isBeingSaved = false;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) => propertiesEditor.ResetResource();
}
