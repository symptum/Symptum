using Symptum.Core.Data;

namespace Symptum.Editor.Controls;

public class EditAuthorInfoDialog : ContentDialog, IEditorDialog
{

    private readonly TextBox nameTB;
    private readonly TextBox emailTB;

    public AuthorInfo Author { get; set; }

    public EditorResult Result { get; private set; } = EditorResult.None;

    public EditAuthorInfoDialog()
    {
        this.Style(ThemeResource.Get<Style>("DefaultContentDialogStyle"));
        PrimaryButtonText = "Ok";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
        Title = "Edit Author Info";
        PrimaryButtonClick += EditAuthorInfoDialog_PrimaryButtonClick;
        CloseButtonClick += EditAuthorInfoDialog_CloseButtonClick;
        nameTB = new TextBox().Header("Name");
        emailTB = new TextBox().Header("Email");
        this.Content(
            new StackPanel()
                .Children(nameTB, emailTB)
                .Orientation(Orientation.Vertical)
                .Spacing(16)
        );
    }

    private void EditAuthorInfoDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Author = new(nameTB.Text, emailTB.Text);
        Result = EditorResult.Update;
    }

    private void EditAuthorInfoDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.Cancel;
    }

    public async Task<EditorResult> EditAsync(AuthorInfo? author)
    {
        Author = author ?? new();
        nameTB.Text = Author.Name;
        emailTB.Text = Author.Email;
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }
}