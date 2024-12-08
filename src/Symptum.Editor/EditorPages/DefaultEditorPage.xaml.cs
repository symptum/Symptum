using Symptum.Editor.Common;

namespace Symptum.Editor.EditorPages;

public sealed partial class DefaultEditorPage : EditorPageBase
{
    public DefaultEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.PropertiesIconSource;
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        propertiesEditor.UpdateResource();
        HasUnsavedChanges = false;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) => propertiesEditor.ResetResource();
}
