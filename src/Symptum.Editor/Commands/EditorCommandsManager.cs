using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

public class EditorCommandsManager
{
    private static readonly IEditorCommand _defaultCommand = new NavigateResourcesCommand();

    static EditorCommandsManager()
    {
        Register(new CreateNewQuestionCommand());
    }

    public static List<IEditorCommand> RegisteredCommands = [];

    public static void Register(IEditorCommand command)
    {
        RegisteredCommands.Add(command);
    }

    public static (IEditorCommand? command, IEnumerable<IEditorCommand>? commands, IEnumerable<EditorCommandOption>? options) GetCommandAndOptions(string? text)
    {
        IEditorCommand? command = _defaultCommand;
        List<IEditorCommand>? commands = [];
        string? parameter = text;
        
        // If there is no '>' at the start we use default command with the whole text as the parameter.
        if (!string.IsNullOrEmpty(text) && text[0] == '>')
        {
            // If the text starts with '>', it is a command
            string key = text[1..].Trim();
            int i = text.IndexOf(' ');
            // If there is a space, we split the command and the parameter
            if (i > 0)
            {
                key = text[1..i].Trim();
                parameter = text[(i + 1)..].Trim();
            }

            Type? type = EditorPagesManager.CurrentEditor?.GetType();
            foreach (IEditorCommand cmd in RegisteredCommands)
            {
                if (cmd.Key.Equals(key))
                {
                    command = cmd;
                    commands = null;
                    break;
                }

                // If the current editor is not null, we filter the commands based on the editor type
                // If the command's page type is null, it means it supports all page types
                if (cmd.Key.Contains(key, StringComparison.InvariantCultureIgnoreCase) &&
                    (cmd.EditorPageType == null || cmd.EditorPageType == type))
                    commands.Add(cmd);
            }
        }

        IEnumerable<EditorCommandOption>? options = command?.GetOptions(parameter);
        return (command, commands, options);
    }
}

public record EditorCommandOption(string? Title, object? Parameter);
