namespace Symptum.Common.Services;

public static class SettingsService
{
    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    private const string ThemeKey = "AppTheme";
    private const string AccentColorKey = "AccentColor";
    private const string UseSystemAccentKey = "UseSystemAccentColor";
    private const string ReaderThemeKey = "ReaderThemePreset";
    private const string MarkdownFontFamilyKey = "MarkdownFontFamily";
    private const string MarkdownFontSizeKey = "MarkdownFontSize";

    public static int AppTheme
    {
        get => (int)(LocalSettings.Values[ThemeKey] ?? 0);
        set => LocalSettings.Values[ThemeKey] = value;
    }

    public static string? AccentColor
    {
        get => LocalSettings.Values[AccentColorKey] as string;
        set => LocalSettings.Values[AccentColorKey] = value;
    }

    public static bool UseSystemAccentColor
    {
        get => (bool)(LocalSettings.Values[UseSystemAccentKey] ?? true);
        set => LocalSettings.Values[UseSystemAccentKey] = value;
    }

    public static int ReaderThemePreset
    {
        get => (int)(LocalSettings.Values[ReaderThemeKey] ?? -1);
        set => LocalSettings.Values[ReaderThemeKey] = value;
    }

    public static int MarkdownFontFamily
    {
        get => (int)(LocalSettings.Values[MarkdownFontFamilyKey] ?? 0);
        set => LocalSettings.Values[MarkdownFontFamilyKey] = value;
    }

    public static double MarkdownFontSize
    {
        get => (double)(LocalSettings.Values[MarkdownFontSizeKey] ?? 14.0);
        set => LocalSettings.Values[MarkdownFontSizeKey] = value;
    }
}
