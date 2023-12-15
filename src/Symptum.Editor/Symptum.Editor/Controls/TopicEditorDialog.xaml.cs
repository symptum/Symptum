namespace Symptum.Editor.Controls;

public sealed partial class TopicEditorDialog : ContentDialog
{
    public string TopicName { get; private set; }

    public EditorResult Result { get; private set; } = EditorResult.None;

    public TopicEditorDialog()
    {
        InitializeComponent();
        Opened += TopicEditorDialog_Opened;
        PrimaryButtonClick += TopicEditorDialog_PrimaryButtonClick;
        CloseButtonClick += TopicEditorDialog_CloseButtonClick;
    }

    private void TopicEditorDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = EditorResult.Cancel;
    }

    private void TopicEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = ValidateTopicName();

        if (args.Cancel == false)
        {
            Result = EditorResult.Create;
            TopicName = topicNameTextBox.Text;
        }
    }

    private void TopicEditorDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        topicNameTextBox.Text = TopicName;
        ValidateTopicName();
    }

    private bool ValidateTopicName()
    {
        return errorInfoBar.IsOpen = string.IsNullOrEmpty(topicNameTextBox.Text);
    }

    public async Task<EditorResult> CreateAsync()
    {
        Title = "Add a New Topic";
        TopicName = string.Empty;
        await ShowAsync(ContentDialogPlacement.Popup);
        return Result;
    }

    //public async Task<EditorResult> EditAsync(string topicName, string dialogTitle = "Edit Topic")
    //{
    //    Title = dialogTitle;
    //    TopicName = topicName;
    //    await ShowAsync(ContentDialogPlacement.Popup);
    //    return Result;
    //}
}
