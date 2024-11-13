namespace Symptum.Editor.Controls;

public class NewItemType
{
    public string? DisplayName { get; set; }

    public Type? Type { get; set; }

    public string? GroupName { get; set; }

    public NewItemType()
    { }

    public NewItemType(string displayName, Type type, string? groupName = null)
    {
        DisplayName = displayName;
        Type = type;
        GroupName = groupName;
    }
}
