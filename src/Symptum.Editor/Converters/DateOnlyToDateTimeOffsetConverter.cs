using Microsoft.UI.Xaml.Data;
using System;

namespace Symptum.Editor.Converters
{
    public class DateOnlyToDateTimeOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateOnly dateOnly)
            {
                return new DateTimeOffset(dateOnly.ToDateTime(new TimeOnly(0)));
            }
            else return DateTimeOffset.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                return DateOnly.FromDateTime(dateTimeOffset.Date);
            }
            else return DateOnly.FromDateTime(DateTime.Now);
        }
    }
}