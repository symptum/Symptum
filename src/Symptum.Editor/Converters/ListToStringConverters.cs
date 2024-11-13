using Microsoft.UI.Xaml.Data;
using Symptum.Core.Data.Bibliography;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Data.ReferenceValues;
using static Symptum.Core.TypeConversion.ListToStringConversion;

namespace Symptum.Editor.Converters;

public class DateOnlyListToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<DateOnly>(value, ElementToStringForDateOnly);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<DateOnly>(value.ToString(), ValidateDataForDate);
    }
}

public class StringListToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<string>(value, ElementToStringDefault);
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<string>(value.ToString(), ValidateDataForString);
    }
}

public class ReferenceListToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<ReferenceBase>(value, x => x?.GetPreviewText() ?? string.Empty, "\n");
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return new NotImplementedException();
    }
}

public class ReferenceValueEntryListToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<ReferenceValueEntry>(value, x => x?.GetPreviewText() ?? string.Empty, "\n");
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class FoodMeasureListToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<FoodMeasure>(value, x => x?.GetPreviewText() ?? string.Empty, "\n");
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
