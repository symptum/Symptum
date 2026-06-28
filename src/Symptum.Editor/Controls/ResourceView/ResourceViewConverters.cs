namespace Symptum.Editor.Controls;

public class DepthToWidthConverter : IValueConverter
{
    public double Multiplier { get; set; } = 8;

    public object Convert(object value, Type targetType, object parameter, string language) =>
        (value is int depth ? depth : 0) * Multiplier + 24;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class BooleanToResourceViewSelectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            true => ResourceViewSelectionMode.Multiple,
            false => ResourceViewSelectionMode.Single,
            _ => ResourceViewSelectionMode.None,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToRotationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        (value is bool isExpanded && isExpanded) ? 90.0 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class SelectionModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ResourceViewSelectionMode mode && mode == ResourceViewSelectionMode.Multiple
            ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
