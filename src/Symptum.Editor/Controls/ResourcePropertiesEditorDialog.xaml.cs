using Symptum.Core.Management.Resources;

namespace Symptum.Editor.Controls;

public sealed partial class ResourcePropertiesEditorDialog : ContentDialog
{
    public IResource? Resource { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    public ResourcePropertiesEditorDialog()
    {
        InitializeComponent();

        Opened += ResourcePropertiesEditorDialog_Opened;
        PrimaryButtonClick += ResourcePropertiesEditorDialog_PrimaryButtonClick;
        CloseButtonClick += ResourcePropertiesEditorDialog_CloseButtonClick;
    }

    private void ResourcePropertiesEditorDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
        propertiesEditor.ResetResource();
    }

    private void ResourcePropertiesEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Update;
        propertiesEditor.UpdateResource();
    }

    private void ResourcePropertiesEditorDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        propertiesEditor.Resource = Resource;
    }

    public async Task<EditorResult> EditAsync(IResource resource)
    {
        Title = "Edit Properties";
        PrimaryButtonText = "Update";
        Resource = resource;
        await ShowAsync();
        return EditResult;
    }
}
