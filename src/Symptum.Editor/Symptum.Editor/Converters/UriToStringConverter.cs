using Microsoft.UI.Xaml.Data;
using Symptum.Core.Management.Resource;
using System;

namespace Symptum.Editor.Converters;

public class UriToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Uri uri)
        {
            return uri.ToString();
        }
        else return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string text && !string.IsNullOrEmpty(text))
        {
            return new Uri(text);
        }
        else return ResourceManager.DefaultUri;
    }
}