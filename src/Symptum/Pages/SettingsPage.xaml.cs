using Symptum.Helpers;
using Symptum.ViewModels;

namespace Symptum.Pages;

public sealed partial class SettingsPage : NavigablePage
{
    public FocusSessionViewModel FocusSession => FocusSessionViewModel.Instance;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        themeCB.SelectedIndex = (int)ThemeHelper.AppTheme;
        readerThemeCB.ItemsSource = ThemeHelper.ReaderThemeNames;
        readerThemeCB.SelectedItem = ThemeHelper.ReaderTheme;
        themeCB.SelectionChanged += ThemeCB_SelectionChanged;
        readerThemeCB.SelectionChanged += ReaderThemeCB_SelectionChanged;
        fontFamilyCB.ItemsSource = ThemeHelper.FontNames;
        fontFamilyCB.SelectedItem = ThemeHelper.FontName;
        fontFamilyCB.SelectionChanged += FontFamilyCB_SelectionChanged;
        fontSizeSlider.Value = ThemeHelper.FontSize;
        fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
    }

    private void ThemeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyThemePreference();
    }

    private void ReaderThemeCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyThemePreference();
    }

    private void ApplyThemePreference()
    {
        ElementTheme theme = (ElementTheme)(themeCB?.SelectedIndex ?? 0);
        string rtname = readerThemeCB?.SelectedItem?.ToString() ?? "Default";
        ThemeHelper.ApplyTheme(rtname, theme);
    }

    private void FontFamilyCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ThemeHelper.ApplyFontFamily(fontFamilyCB.SelectedItem?.ToString() ?? "Default");
    }

    private void FontSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        ThemeHelper.ApplyFontSize(e.NewValue);
    }
}
