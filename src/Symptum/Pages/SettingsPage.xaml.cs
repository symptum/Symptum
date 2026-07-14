using Symptum.Common.Services;
using Symptum.Services;
using Windows.UI;

namespace Symptum.Pages;

public sealed partial class SettingsPage : NavigablePage
{
    private bool _isLoading;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;

        themeCombo.SelectedIndex = SettingsService.AppTheme;

        systemAccentToggle.IsOn = SettingsService.UseSystemAccentColor;
        accentColorButton.ColorPicker.Color = GetCurrentAccentColor();
        accentColorButton.ColorPicker.ColorChanged += OnAccentColorChanged;

        readerThemeCombo.SelectedIndex = SettingsService.ReaderThemePreset + 1;

        fontCombo.SelectedIndex = SettingsService.MarkdownFontFamily;
        sizeCombo.SelectedIndex = FontSizeToIndex(SettingsService.MarkdownFontSize);

        _isLoading = false;
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        var theme = themeCombo.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        SettingsService.AppTheme = themeCombo.SelectedIndex;
        ThemeService.SetTheme(theme);
    }

    private void OnSystemAccentToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        accentColorButton.IsEnabled = !systemAccentToggle.IsOn;

        if (systemAccentToggle.IsOn)
        {
            ThemeService.UseSystemAccent();
        }
    }

    private void OnAccentColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isLoading) return;

        ThemeService.SetAccentColor(args.NewColor);
    }

    private void OnReaderThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        var presetIndex = readerThemeCombo.SelectedIndex - 1;
        SettingsService.ReaderThemePreset = presetIndex;

        if (presetIndex < 0)
            ThemeService.ClearReaderTheme();
        else
            ThemeService.ApplyReaderThemePreset(presetIndex);
    }

    private void OnFontChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        SettingsService.MarkdownFontFamily = fontCombo.SelectedIndex;
    }

    private void OnSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        SettingsService.MarkdownFontSize = IndexToFontSize(sizeCombo.SelectedIndex);
    }

    private Color GetCurrentAccentColor()
    {
        if (!systemAccentToggle.IsOn &&
            !string.IsNullOrEmpty(SettingsService.AccentColor) &&
            TryParseHex(SettingsService.AccentColor, out var custom))
        {
            return custom;
        }
        return ThemeService.GetSystemAccentColor();
    }

    private static int FontSizeToIndex(double size) => size switch
    {
        10 => 0,
        12 => 1,
        14 => 2,
        16 => 3,
        18 => 4,
        20 => 5,
        _ => 2
    };

    private static double IndexToFontSize(int index) => index switch
    {
        0 => 10,
        1 => 12,
        2 => 14,
        3 => 16,
        4 => 18,
        5 => 20,
        _ => 14
    };

    private static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        hex = hex.TrimStart('#');
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8) return false;

        try
        {
            byte a = Convert.ToByte(hex[..2], 16);
            byte r = Convert.ToByte(hex[2..4], 16);
            byte g = Convert.ToByte(hex[4..6], 16);
            byte b = Convert.ToByte(hex[6..8], 16);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }
        catch { return false; }
    }
}
