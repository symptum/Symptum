using Microsoft.UI.Xaml.Data;

namespace Symptum.Editor.Converters;

public class InvertBooleanToVisibilityConverter : IValueConverter
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

        return result ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
