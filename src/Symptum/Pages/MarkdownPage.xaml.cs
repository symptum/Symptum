using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;

namespace Symptum.Pages;

public sealed partial class MarkdownPage : NavigablePage
{
    private MarkdownFileResource? resource;

    private static readonly ResourceDictionary[] CustomThemes =
    [
        new() { Source = new Uri("ms-appx:///Themes/ReaderThemes/SepiaTheme.xaml") },
        new() { Source = new Uri("ms-appx:///Themes/ReaderThemes/WarmLightTheme.xaml") },
        new() { Source = new Uri("ms-appx:///Themes/ReaderThemes/NordTheme.xaml") },
        new() { Source = new Uri("ms-appx:///Themes/ReaderThemes/DraculaTheme.xaml") },
        new() { Source = new Uri("ms-appx:///Themes/ReaderThemes/OLEDDarkTheme.xaml") },
    ];

    public MarkdownPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is MarkdownFileResource md)
        {
            resource = md;
            markdownView.Text = md.Markdown;
            ApplyTheme(themeCombo.SelectedIndex);
        }
    }

    private void OnSettingChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFontResources();
        ReRenderMarkdown();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTheme(themeCombo.SelectedIndex);
    }

    private void ApplyTheme(int index)
    {
        RemoveCustomDictionary();

        ElementTheme desiredTheme = index switch
        {
            0 => ElementTheme.Default,
            1 => ElementTheme.Light,
            2 => ElementTheme.Light,
            3 => ElementTheme.Light,
            4 => ElementTheme.Dark,
            5 => ElementTheme.Dark,
            6 => ElementTheme.Dark,
            7 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (index is >= 2 and <= 3)
            App.Current.Resources.MergedDictionaries.Add(CustomThemes[index - 2]);
        else if (index is >= 5 and <= 7)
            App.Current.Resources.MergedDictionaries.Add(CustomThemes[index - 3]);

        if (XamlRoot is { } xamlRoot)
        {
            ElementTheme opposite = desiredTheme == ElementTheme.Light
                ? ElementTheme.Dark : ElementTheme.Light;
            SystemThemeHelper.SetApplicationTheme(xamlRoot, opposite);
            SystemThemeHelper.SetApplicationTheme(xamlRoot, desiredTheme);
        }

        ApplyFontResources();
    }

    private void RemoveCustomDictionary()
    {
        var merged = App.Current.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i].Source?.OriginalString?.Contains("/ReaderThemes/") == true)
            {
                merged.RemoveAt(i);
            }
        }
    }

    private static readonly double[] FontSizes = [10, 12, 14, 16, 18, 20];

    private void ApplyFontResources()
    {
        string? fontFamily = fontCombo.SelectedIndex switch
        {
            1 => "Georgia, 'Times New Roman', 'DejaVu Serif', 'Liberation Serif'",
            2 => "Arial, Helvetica, 'DejaVu Sans', 'Liberation Sans'",
            3 => "Consolas, 'Courier New', 'DejaVu Sans Mono', 'Liberation Mono'",
            _ => null
        };

        double fontSize = FontSizes[sizeCombo.SelectedIndex];
        double scale = fontSize / 14.0;

        if (fontFamily != null)
            Resources["MarkdownBodyFontFamily"] = new FontFamily(fontFamily);
        else
            Resources.Remove("MarkdownBodyFontFamily");

        Resources["MarkdownBodyFontSize"] = fontSize;
        Resources["MarkdownH1FontSize"] = 28.0 * scale;
        Resources["MarkdownH2FontSize"] = 24.0 * scale;
        Resources["MarkdownH3FontSize"] = 22.0 * scale;
        Resources["MarkdownH4FontSize"] = 20.0 * scale;
        Resources["MarkdownH5FontSize"] = 18.0 * scale;
        Resources["MarkdownH6FontSize"] = 16.0 * scale;

        // Set at Application level too so it's found even when re-rendered elements
        // aren't yet attached to the visual tree during style resolution.
        App.Current.Resources["MarkdownBodyFontSize"] = fontSize;
        App.Current.Resources["MarkdownH1FontSize"] = Resources["MarkdownH1FontSize"];
        App.Current.Resources["MarkdownH2FontSize"] = Resources["MarkdownH2FontSize"];
        App.Current.Resources["MarkdownH3FontSize"] = Resources["MarkdownH3FontSize"];
        App.Current.Resources["MarkdownH4FontSize"] = Resources["MarkdownH4FontSize"];
        App.Current.Resources["MarkdownH5FontSize"] = Resources["MarkdownH5FontSize"];
        App.Current.Resources["MarkdownH6FontSize"] = Resources["MarkdownH6FontSize"];
    }

    private void ReRenderMarkdown()
    {
        if (resource != null)
        {
            var text = resource.Markdown;
            markdownView.Text = string.Empty;
            markdownView.Text = text;
        }
    }
}
