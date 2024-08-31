using Microsoft.UI.Xaml.Data;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;

namespace Symptum.Editor.EditorPages;

public sealed partial class DefaultEditorPage : EditorPageBase
{
    public DefaultEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.PropertiesIconSource;
    }

    protected override void OnSetEditableContent(IResource? resource)
    {
        propertiesEditor.Resource = resource;
        var binding = new Binding { Path = new PropertyPath(nameof(Title)), Source = resource };
        SetBinding(TitleProperty, binding);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        propertiesEditor.UpdateResource();
        HasUnsavedChanges = false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        propertiesEditor.ResetResource();
    }
}
