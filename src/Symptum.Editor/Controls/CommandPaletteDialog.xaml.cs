using Symptum.Editor.Commands;

namespace Symptum.Editor.Controls;

public sealed partial class CommandPaletteDialog : ContentDialog
{
    private IEditorCommand? _selectedCommand;

    public CommandPaletteDialog()
    {
        InitializeComponent();
        Opened += (s, e) =>
        {
            commandBox.Text = null;
        };

        commandBox.TextChanged += CommandBox_TextChanged;

        commandBox.SuggestionChosen += CommandBox_SuggestionChosen;
    }

    private void CommandBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        LoadOptions(sender.Text);
    }

    private void CommandBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is IEditorCommand cmd)
             commandBox.Text = ">" + cmd.Key + " ";
    }

    public async Task ExecuteAsync()
    {
        var result = await ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (optionsLV.SelectedItem is EditorCommandOption option)
                _selectedCommand?.Execute(option);
        }
    }

    private void LoadOptions(string? queryText = null)
    {
        (IEditorCommand? cmd, IEnumerable<IEditorCommand>? cmds, IEnumerable<EditorCommandOption>? opts) =
            EditorCommandsManager.GetCommandAndOptions(queryText);

        _selectedCommand = cmd;
        commandBox.ItemsSource = cmds;
        List<EditorCommandOption>? options = opts?.ToList();
        optionsLV.ItemsSource = options;
        optionsLV.SelectedItem = options?.FirstOrDefault();
    }
}
