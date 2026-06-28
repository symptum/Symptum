namespace Symptum.Editor.Controls;

public class ListEditorItemActionRequestedEventArgs(ListEditorItemActionType actionType, object? args = null)
{
    public ListEditorItemActionType ActionType { get; } = actionType;

    public object? Arguments { get; } = null;
}
