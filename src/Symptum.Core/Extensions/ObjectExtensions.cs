namespace Symptum.Core.Extensions;

public static class ObjectExtensions
{
    public static bool IsNullOrDefault(this object? value)
    {
        return value == null || value == default || (value is string s && s.IsNullOrEmptyOrWhiteSpace());
    }

    public static bool IsNullOrEmptyOrWhiteSpace(this string? value)
    {
        return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
    }
}
