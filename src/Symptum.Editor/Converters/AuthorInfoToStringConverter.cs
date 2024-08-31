using Microsoft.UI.Xaml.Data;
using Symptum.Core.Data;

namespace Symptum.Editor.Converters;

public class AuthorInfoToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (AuthorInfo.TryParse(value as string, out AuthorInfo authorInfo))
            return authorInfo;

        return default(AuthorInfo);
    }
}
