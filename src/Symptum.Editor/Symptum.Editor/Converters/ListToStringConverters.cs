using Microsoft.UI.Xaml.Data;
using Symptum.Core.Subjects.Books;
using static Symptum.Core.TypeConversion.ListToStringConversion;

namespace Symptum.Editor.Converters;

public class DateOnlyListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<DateOnly>(value, ElementToStringForDateOnly);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<DateOnly>(value.ToString(), ValidateDataForDate);
    }
}

public class UriListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<Uri>(value, ElementToStringDefault);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<Uri>(value.ToString(), ValidateDataForUri);
    }
}

public class StringListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<string>(value, ElementToStringDefault);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<string>(value.ToString(), ValidateDataForString);
    }
}

public class BookLocationListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ConvertToString<BookLocation>(value, ElementToStringDefault);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return ConvertFromString<BookLocation>(value.ToString(), ValidateDataForBookLocation);
    }
}
