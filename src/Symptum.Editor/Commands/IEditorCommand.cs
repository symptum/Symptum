namespace Symptum.Editor.Commands;

public interface IEditorCommand
{
    public string Key { get; }

    public Type? EditorPageType { get; }

    public int NumberOfArguments { get; }

    public void Execute(IEnumerable<EditorCommandArgument>? args = null);

    public Task<EditorCommandArgumentRequest?> GetRequestAsync(string? text = null, int argIndex = 0);
}

public enum EditorCommandArgumentRequestType
{
    Text, // Simple text input
    Option, // List of options to choose from
    Search // List of options to choose from, but with a search box
}

// If the request type is Text, it will prompt the user for a text input
// If the request type is Option, it will prompt the user for a selection from a list of options
public class EditorCommandArgumentRequest(string? title, EditorCommandArgumentRequestType requestType,
    IEnumerable<EditorCommandArgument>? options = null)
{
    public string? Title { get; } = title;

    public EditorCommandArgumentRequestType RequestType { get; } = requestType;

    public IEnumerable<EditorCommandArgument>? Options { get; } = options;
}

public class EditorCommandArgument(string? title, object? parameter)
{
    public string? Title { get; } = title;

    public object? Parameter { get; } = parameter;
}
