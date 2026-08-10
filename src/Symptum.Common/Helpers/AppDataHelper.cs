using System.Runtime.CompilerServices;

namespace Symptum.Common.Helpers;

/// <summary>
/// Helper for reading and writing application local settings using
/// <see cref="ApplicationData.Current.LocalSettings"/>. Values are stored as
/// strings and this class provides typed accessors that convert common types
/// (string, bool, int, double and enums) when reading.
/// </summary>
public static class AppDataHelper
{
    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    /// <summary>
    /// Retrieves a typed value from local settings. If the setting does not
    /// exist or cannot be converted, the provided <paramref name="defaultValue"/>
    /// is returned.
    /// </summary>
    /// <typeparam name="T">Requested return type. Supported conversions: string,
    /// bool, int, double and enum types.</typeparam>
    /// <param name="defaultValue">Value to return when the setting is not
    /// present or conversion fails.</param>
    /// <param name="propertyName">Name of the setting. When omitted the
    /// caller member name will be used (CallerMemberName).</param>
    /// <returns>The converted setting value or <paramref name="defaultValue"/>.</returns>
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

    /// <summary>
    /// Stores a value in local settings. The value is written as its string
    /// representation. The <paramref name="propertyName"/> defaults to the
    /// caller member name when not supplied.
    /// </summary>
    /// <typeparam name="T">Type of the value to store.</typeparam>
    /// <param name="value">Value to store.</param>
    /// <param name="propertyName">Optional setting name (defaults to caller
    /// member name).</param>
    public static void SetValue<T>(T value, [CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        LocalSettings.Values[propertyName] = value?.ToString();
    }
}
