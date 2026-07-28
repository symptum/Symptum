using System.Runtime.CompilerServices;

namespace Symptum.Common.Helpers;

public static class AppDataHelper
{
    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    public static T GetValue<T>(T defaultValue, [CallerMemberName] string? propertyName = null)
    {
        if (!string.IsNullOrEmpty(propertyName) &&
            LocalSettings.Values.TryGetValue(propertyName, out var value))
        {
            if (typeof(T) == typeof(string))
            {
                return (T)value;
            }
            else if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(value.ToString(), out var result))
                {
                    return (T)(object)result;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    return (T)(object)result;
                }
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value.ToString(), out double result))
                {
                    return (T)(object)result;
                }
            }
            else if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), value.ToString() ?? string.Empty);
            }
        }

        return defaultValue;
    }

    public static void SetValue<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        LocalSettings.Values[propertyName] = value?.ToString();
    }
}
