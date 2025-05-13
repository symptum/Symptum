namespace Symptum.Editor.Commands;

public interface IEditorCommand
{
    public string Key { get; }

    public Type? EditorPageType { get; }

    public void Execute(EditorCommandOption? option = null);

    public IEnumerable<EditorCommandOption> GetOptions(string? parameter = null);
}
