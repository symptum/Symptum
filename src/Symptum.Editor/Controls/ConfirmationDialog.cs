namespace Symptum.Editor.Controls;

public class ConfirmationDialog : ContentDialog, IEditorDialog
{
    private enum ConfirmationType
    {
        Deletion,
        Closing
    }

    private ConfirmationType _confirmationType;

    public EditorResult Result { get; set; } = EditorResult.None;

    public ConfirmationDialog()
    {
        this.Style(ThemeResource.Get<Style>("DefaultContentDialogStyle"));
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
        PrimaryButtonClick += ConfirmationDialog_PrimaryButtonClick;
        SecondaryButtonClick += ConfirmationDialog_SecondaryButtonClick;
        CloseButtonClick += ConfirmationDialog_CloseButtonClick;
    }

    private void ConfirmationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = _confirmationType switch
        {
            ConfirmationType.Deletion => EditorResult.Delete,
            ConfirmationType.Closing => EditorResult.Update,
            _ => EditorResult.None
        };
    }

    private void ConfirmationDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.None;
    }

    private void ConfirmationDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.Cancel;
    }


    public async Task<EditorResult> ConfirmDeletionAsync(string type)
    {
        Title = $"Delete {type}?";
        Content = $"Do you want to delete the {type}?\nOnce you delete you won't be able to restore.";
        PrimaryButtonText = "Delete";
        SecondaryButtonText = null;
        _confirmationType = ConfirmationType.Deletion;
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }

    public async Task<EditorResult> ConfirmClosingUnsavedAsync(string title)
    {
        Title = $"Unsaved changes in {title}!";
        Content = $"Do you want to save the changes made to {title}?\nYour changes will be lost if not saved.";
        PrimaryButtonText = "Save";
        SecondaryButtonText = "Don't Save";
        _confirmationType = ConfirmationType.Closing;
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }
}
