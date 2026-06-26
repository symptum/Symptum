namespace Symptum.Editor.Converters;

public class BooleanToTreeViewSelectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            true => TreeViewSelectionMode.Multiple,
            false => TreeViewSelectionMode.Single,
            _ => TreeViewSelectionMode.None,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}