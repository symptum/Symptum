namespace Symptum.Editor.Controls;

public enum EditResult
{
    None,
    Save,
    Cancel
}

public sealed partial class TopicEditorDialog : ContentDialog
{
    public string TopicName { get; private set; }

    public EditResult EditResult { get; private set; } = EditResult.None;

    public TopicEditorDialog()
    {
        this.InitializeComponent();
        Opened += TopicEditorDialog_Opened;
        PrimaryButtonClick += TopicEditorDialog_PrimaryButtonClick;
        CloseButtonClick += TopicEditorDialog_CloseButtonClick;
    }

    private void TopicEditorDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditResult.Cancel;
    }

    private void TopicEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = ValidateTopicName();

        if (args.Cancel == false)
        {
            EditResult = EditResult.Save;
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

    public async Task<EditResult> CreateAsync()
    {
        return await EditAsync(string.Empty, "Add a New Topic");
    }

    public async Task<EditResult> EditAsync(string topicName, string dialogTitle = "Edit Topic")
    {
        Title = dialogTitle;
        TopicName = topicName;
        await ShowAsync(ContentDialogPlacement.Popup);
        return EditResult;
    }
}
