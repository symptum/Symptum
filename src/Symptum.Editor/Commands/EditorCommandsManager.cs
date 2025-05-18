using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

public class EditorCommandsManager
{
    private static readonly IEditorCommand _defaultCommand = new NavigateResourcesCommand();

    static EditorCommandsManager()
    {
        Register(new CreateNewQuestionCommand());
    }

    public static IEditorCommand DefaultCommand { get => _defaultCommand; }

    public static List<IEditorCommand> RegisteredCommands { get; } = [];

    public static void Register(IEditorCommand command)
    {
        RegisteredCommands.Add(command);
    }

    public static IEnumerable<IEditorCommand>? GetCommandsByKey(string? key)
    {
        List<IEditorCommand>? matches = [];
        Type? type = EditorPagesManager.CurrentEditor?.GetType();
        foreach (IEditorCommand cmd in RegisteredCommands)
        {
            // If the current editor is not null, we filter the commands based on the editor type
            // If the command's page type is null, it means it supports all page types
            if (cmd.Key.Contains(key, StringComparison.InvariantCultureIgnoreCase) &&
                (cmd.EditorPageType == null || cmd.EditorPageType == type))
                matches.Add(cmd);
        }

        return matches;
    }
}
