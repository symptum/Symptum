using Symptum.Editor.Commands;
using Windows.System;

namespace Symptum.Editor.Controls;

public sealed partial class CommandPaletteControl : UserControl
{
    private IEditorCommand _selectedCommand = EditorCommandsManager.DefaultCommand;
    private bool _isInCommandContext = false;
    private bool _isInSearchContext = false;

    public CommandPaletteControl()
    {
        InitializeComponent();
    }

    private void CommandBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            HandleTextChange(sender.Text);
    }

    private void CommandBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is IEditorCommand cmd)
            sender.Text = ">" + cmd.Key;
    }

    private void CommandBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is IEditorCommand cmd)
            SelectCommand(cmd);
        else
            HandleSubmit(args.QueryText);
    }

    private void CommandBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (_isInCommandContext)
                ResetPalette();
            else
                Visibility = Visibility.Collapsed;
        }
    }

    private void OptionsLV_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is EditorCommandArgument arg)
            PushArgumentAndMove(arg);
    }

    public void ShowPalette()
    {
        if (Visibility == Visibility.Visible) return;

        commandBox.Text = null;
        Visibility = Visibility.Visible;
        commandBox.Focus(FocusState.Programmatic);
        ResetPalette();
    }

    private void SelectCommand(IEditorCommand? cmd)
    {
        if (cmd == null) return;
        _selectedCommand = cmd;
        _isInCommandContext = true;
        commandBox.Text = null;
        commandBox.ItemsSource = null;
        LoadCommandContextAsync();
    }

    private void HandleTextChange(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // If not already in a command's context, it means the user is either typing a command's key
        // or the parameter for the default command.
        // If the text starts with '>', it is a command
        if (text[0] == '>')
        {
            if (!_isInCommandContext)
            {
                string key = text[1..].Trim();
                var matches = EditorCommandsManager.GetCommandsByKey(key);
                commandBox.ItemsSource = matches?.ToList();
            }
            else ResetPalette(); // User wants to type a new command while already in a command's context;
        }
        else
        {
            // If we are in a command's context, we need to pass the text to the command to process
            commandBox.ItemsSource = null;
            if (!_isInCommandContext || _isInSearchContext) LoadCommandContextAsync(text);
            _isInCommandContext = true;
        }
    }

    private void HandleSubmit(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Just to be sure the submit was not triggered by the command selection
        if (text[0] != '>')
        {
            EditorCommandArgument? arg = _isInSearchContext ? optionsLV.SelectedItem as EditorCommandArgument
                : new EditorCommandArgument(null, text);
            PushArgumentAndMove(arg);
        }
        else
        {
            string key = text[1..];
            var cmd = EditorCommandsManager.RegisteredCommands.FirstOrDefault(c => c.Key.Equals(key));
            SelectCommand(cmd);
        }
    }

    private void PushArgumentAndMove(EditorCommandArgument? arg)
    {
        if (arg == null) return;

        _currentArguments.Add(arg);
        _i++; // Move to the next argument

        if (_i >= _selectedCommand.NumberOfArguments)
        {
            _selectedCommand?.Execute(_currentArguments);
            Visibility = Visibility.Collapsed;
        }
        else LoadCommandContextAsync();
    }

    private int _i = 0;
    private List<EditorCommandArgument> _currentArguments = [];

    private async void LoadCommandContextAsync(string? parameter = null)
    {
        if (_selectedCommand != null)
        {
            _isInSearchContext = false;

            var request = await _selectedCommand.GetRequestAsync(parameter, _i);
            if (request == null) return;
            titleTB.Text = request.Title;
            var type = request.RequestType;
            if (type == EditorCommandArgumentRequestType.Text)
            {
                commandBox.Text = null;
                commandBox.Visibility = Visibility.Visible;
                optionsLV.ItemsSource = null;
                optionsLV.Visibility = Visibility.Collapsed;
                commandBox.Focus(FocusState.Programmatic);
            }
            else
            {
                var options = request.Options?.ToList();
                optionsLV.ItemsSource = options;
                optionsLV.SelectedItem = options?.FirstOrDefault();
                optionsLV.Visibility = Visibility.Visible;
                if (request.RequestType == EditorCommandArgumentRequestType.Search)
                {
                    _isInSearchContext = true;
                    commandBox.Visibility = Visibility.Visible;
                    commandBox.Focus(FocusState.Programmatic);
                }
                else
                {
                    commandBox.Visibility = Visibility.Collapsed;
                    Focus(FocusState.Programmatic);
                    optionsLV.Focus(FocusState.Programmatic);
                }
            }
        }
    }

    private void ResetPalette()
    {
        titleTB.Text = "Command Palette";
        commandBox.ItemsSource = null;
        commandBox.Visibility = Visibility.Visible;
        optionsLV.ItemsSource = null;
        optionsLV.Visibility = Visibility.Collapsed;
        _selectedCommand = EditorCommandsManager.DefaultCommand;
        _currentArguments.Clear();
        _i = 0;
        _isInCommandContext = false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }
}
