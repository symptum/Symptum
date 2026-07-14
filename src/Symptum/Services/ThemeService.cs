using Symptum.Common.Services;
using Symptum.Themes;
using Windows.UI;

namespace Symptum.Services;

public static class ThemeService
{
    public static event EventHandler? ThemeChanged;

    private static FrameworkElement? _rootElement;

    public static void Initialize(FrameworkElement rootElement)
    {
        _rootElement = rootElement;
    }

    public static void LoadTheme()
    {
        var theme = SettingsService.AppTheme switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        SetTheme(theme);

        if (!SettingsService.UseSystemAccentColor)
        {
            var hex = SettingsService.AccentColor;
            if (!string.IsNullOrEmpty(hex) && TryParseColor(hex, out var color))
            {
                ApplyAccentColor(color);
            }
        }

        var presetIndex = SettingsService.ReaderThemePreset;
        if (presetIndex >= 0)
        {
            ApplyReaderThemePreset(presetIndex);
        }
    }

    public static void SetTheme(ElementTheme theme)
    {
        if (_rootElement is { } root)
        {
            SystemThemeHelper.SetApplicationTheme(root.XamlRoot, theme);
        }

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static ElementTheme GetTheme()
    {
        if (_rootElement is { } root)
            return (ElementTheme)SystemThemeHelper.GetRootTheme(root.XamlRoot);

        return ElementTheme.Default;
    }

    public static void SetAccentColor(Color color)
    {
        SettingsService.UseSystemAccentColor = false;
        SettingsService.AccentColor = ColorToHex(color);
        ApplyAccentColor(color);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void UseSystemAccent()
    {
        SettingsService.UseSystemAccentColor = true;
        SettingsService.AccentColor = null;
        ClearAccentOverrides();
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static Color GetSystemAccentColor()
    {
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        return uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
    }

    private static void ApplyAccentColor(Color color)
    {
        var resources = Application.Current.Resources;
        resources["SystemAccentColor"] = color;
        resources["SystemAccentColorLight1"] = Lighten(color, 0.2);
        resources["SystemAccentColorLight2"] = Lighten(color, 0.35);
        resources["SystemAccentColorLight3"] = Lighten(color, 0.5);
        resources["SystemAccentColorDark1"] = Darken(color, 0.2);
        resources["SystemAccentColorDark2"] = Darken(color, 0.35);
        resources["SystemAccentColorDark3"] = Darken(color, 0.5);
    }

    private static void ClearAccentOverrides()
    {
        var systemAccent = GetSystemAccentColor();
        ApplyAccentColor(systemAccent);
    }

    public static void ApplyReaderThemePreset(int index)
    {
        var presets = ReaderThemePresets.Presets;
        if (index < 0 || index >= presets.Length) return;

        var preset = presets[index];
        var resources = Application.Current.Resources;

        resources["MarkdownBackgroundBrush"] = new SolidColorBrush(preset.Background);
        resources["MarkdownForegroundBrush"] = new SolidColorBrush(preset.Foreground);
        resources["MarkdownAccentBrush"] = new SolidColorBrush(preset.Accent);
        resources["MarkdownH1Foreground"] = new SolidColorBrush(preset.Accent);
        resources["MarkdownCodeBackground"] = new SolidColorBrush(preset.CodeBackground);
        resources["MarkdownCodeBorderBrush"] = new SolidColorBrush(preset.CodeBorder);
        resources["MarkdownTableHeaderBackground"] = new SolidColorBrush(preset.Accent);
        resources["MarkdownTableHeaderForeground"] = new SolidColorBrush(preset.TableHeaderForeground);
        resources["MarkdownTableBorderBrush"] = new SolidColorBrush(preset.TableBorder);
        resources["MarkdownTableAltBackground"] = new SolidColorBrush(preset.TableAltBackground);
        resources["MarkdownThematicBreakBrush"] = new SolidColorBrush(preset.TableBorder);
        resources["DefaultQuoteBackground"] = new SolidColorBrush(preset.QuoteBackground);
        resources["DefaultQuoteBorderBrush"] = new SolidColorBrush(preset.Accent);
        resources["DefaultQuoteForeground"] = new SolidColorBrush(preset.Foreground);

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ClearReaderTheme()
    {
        var resources = Application.Current.Resources;
        resources.Remove("MarkdownBackgroundBrush");
        resources.Remove("MarkdownForegroundBrush");
        resources.Remove("MarkdownAccentBrush");
        resources.Remove("MarkdownH1Foreground");
        resources.Remove("MarkdownCodeBackground");
        resources.Remove("MarkdownCodeBorderBrush");
        resources.Remove("MarkdownTableHeaderBackground");
        resources.Remove("MarkdownTableHeaderForeground");
        resources.Remove("MarkdownTableBorderBrush");
        resources.Remove("MarkdownTableAltBackground");
        resources.Remove("MarkdownThematicBreakBrush");
        resources.Remove("DefaultQuoteBackground");
        resources.Remove("DefaultQuoteBorderBrush");
        resources.Remove("DefaultQuoteForeground");

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex)) return false;

        hex = hex.TrimStart('#');
        if (hex.Length == 6)
            hex = "FF" + hex;
        else if (hex.Length != 8)
            return false;

        try
        {
            byte a = Convert.ToByte(hex[..2], 16);
            byte r = Convert.ToByte(hex[2..4], 16);
            byte g = Convert.ToByte(hex[4..6], 16);
            byte b = Convert.ToByte(hex[6..8], 16);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ColorToHex(Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color Lighten(Color color, double amount)
    {
        byte r = (byte)Math.Min(255, color.R + (255 - color.R) * amount);
        byte g = (byte)Math.Min(255, color.G + (255 - color.G) * amount);
        byte b = (byte)Math.Min(255, color.B + (255 - color.B) * amount);
        return Color.FromArgb(color.A, r, g, b);
    }

    private static Color Darken(Color color, double amount)
    {
        byte r = (byte)(color.R * (1 - amount));
        byte g = (byte)(color.G * (1 - amount));
        byte b = (byte)(color.B * (1 - amount));
        return Color.FromArgb(color.A, r, g, b);
    }
}
