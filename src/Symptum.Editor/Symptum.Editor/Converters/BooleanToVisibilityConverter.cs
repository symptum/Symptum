using Microsoft.UI.Xaml.Data;

namespace Symptum.Editor.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool result = false;
        if (value is bool boolean)
        {
            result = boolean;
        }
        else if (value is bool?)
        {
            bool? nullable = (bool?)value;
            result = nullable.HasValue && nullable.Value;
        }

        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}
