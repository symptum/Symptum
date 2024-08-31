using Microsoft.UI.Xaml.Data;
using Symptum.Core.Math;

namespace Symptum.Editor.Converters;

public class NumericalValueToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (NumericalValue.TryParse(value as string, out NumericalValue numericalValue))
            return numericalValue;

        return default(NumericalValue);
    }
}
