using System.Text;

namespace Symptum.Editor.Controls;

public sealed partial class MarkdownEditorInsertLinkDialog : ContentDialog
{
    public EditorResult EditResult { get; private set; } = EditorResult.None;

    public string Markdown { get; private set; } = string.Empty;

    public MarkdownEditorInsertLinkDialog()
    {
        InitializeComponent();
        Opened += MarkdownEditorInsertLinkDialog_Opened;
        PrimaryButtonClick += MarkdownEditorInsertLinkDialog_PrimaryButtonClick;
        SecondaryButtonClick += MarkdownEditorInsertLinkDialog_SecondaryButtonClick;
    }

    private void MarkdownEditorInsertLinkDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        textTB.Text = null;
        urlTB.Text = null;
        titleTB.Text = null;
    }

    private void MarkdownEditorInsertLinkDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        GenerateMarkdown();
        EditResult = EditorResult.Create;
    }

    private void MarkdownEditorInsertLinkDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
    }

    public async Task<EditorResult> CreateAsync()
    {
        await ShowAsync();
        return EditResult;
    }

    private void GenerateMarkdown()
    {
        StringBuilder result = new();

        string text = textTB.Text;
        string url = urlTB.Text;
        string title = titleTB.Text;

        result.Append('[')
            .Append(text)
            .Append(']')
            .Append('(')
            .Append(url);

        if (!string.IsNullOrWhiteSpace(title))
        {
            result.Append(' ').Append('\"')
                .Append(title).Append('\"');
        }
        result.Append(')');

        Markdown = result.ToString();
    }
}
