using Symptum.Common.Helpers;

namespace Symptum.Helpers;

public static class ThemeHelper
{
    private static bool _initialized = false;

    private static XamlRoot? _root;

    private static readonly List<string> _fontNames =
    [
        "Default",
        "Serif",
        "Sans Serif",
        "Monospace"
    ];

    private static readonly Dictionary<string, FontFamily?> _fonts = new()
    {
        { _fontNames[0], null },
        { _fontNames[1], new("ms-appx:///Assets/Fonts/LiberationSerif-Regular.ttf#Liberation Serif") },
        { _fontNames[2], new("ms-appx:///Assets/Fonts/LiberationSans-Regular.ttf#Liberation Sans") },
        { _fontNames[3], new("ms-appx:///Symptum.UI/Fonts/CascadiaCode-Regular.ttf#Cascadia Code") },
    };

    private static readonly List<string> _readerThemeNames =
    [
         "Default",
         "Forest",
         "Air",
         "Water",
         "Fire",
         "Earth",
    ];

    private static readonly Dictionary<string, ResourceDictionary> _readerThemes = [];

    private static ElementTheme _appTheme = ElementTheme.Default;
    private static string _readerTheme = _readerThemeNames[0];
    private static string _fontName = _fontNames[0];
    private static double _fontSize = 14.0;

    #region Properties

    public static List<string> ReaderThemeNames { get => _readerThemeNames; }

    public static List<string> FontNames { get => _fontNames; }

    public static ElementTheme AppTheme
    {
        get => _initialized ? _appTheme : AppDataHelper.GetValue(_appTheme);
        private set
        {
            _appTheme = value;
            AppDataHelper.SetValue(value);
        }
    }

    public static string ReaderTheme
    {
        get => _initialized ? _readerTheme : AppDataHelper.GetValue(_readerTheme);
        private set
        {
            _readerTheme = value;
            AppDataHelper.SetValue(value);
        }
    }

    public static string FontName
    {
        get => _initialized ? _fontName : AppDataHelper.GetValue(_fontName);
        private set
        {
            _fontName = value;
            AppDataHelper.SetValue(value);
        }
    }

    public static double FontSize
    {
        get => _initialized ? _fontSize : AppDataHelper.GetValue(_fontSize);
        private set
        {
            _fontSize = value;
            AppDataHelper.SetValue(value);
        }
    }

    #endregion

    public static void Initialize(XamlRoot? root)
    {
        _root = root;
        foreach (var name in _readerThemeNames)
        {
            if (name == "Default") continue;
            ResourceDictionary res = new() { Source = new Uri($"ms-appx:///ReaderThemes/{name}.xaml") };
            _readerThemes[name] = res;
        }

        ApplyTheme(ReaderTheme, AppTheme);
        ApplyFontFamily(FontName);
        ApplyFontSize(FontSize);
        _initialized = true;
    }

    public static void ApplyTheme(string readerTheme, ElementTheme appTheme)
    {
        RemoveThemes();

        if (_readerThemes.TryGetValue(readerTheme, out var res))
            App.Current.Resources.MergedDictionaries.Add(res);

        ElementTheme opposite = appTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
        SystemThemeHelper.SetApplicationTheme(_root, opposite);
        SystemThemeHelper.SetApplicationTheme(_root, appTheme);
        AppTheme = appTheme;
        ReaderTheme = readerTheme;
    }

    private static void RemoveThemes()
    {
        // Find any existing ReaderTheme and remove it.
        var merged = App.Current.Resources.MergedDictionaries.Where(r =>
            r.Source?.OriginalString?.Contains("/ReaderThemes/") == true);
        foreach (var res in merged)
        {
            App.Current.Resources.MergedDictionaries.Remove(res);
        }
    }

    public static void ApplyFontFamily(string fontName)
    {
        if (_fonts.TryGetValue(fontName, out FontFamily? fontFamily))
        {
            if (fontFamily != null)
                App.Current.Resources["MarkdownBodyFontFamily"] = fontFamily;
            else
                App.Current.Resources.Remove("MarkdownBodyFontFamily");
            FontName = fontName;
        }
    }

    public static void ApplyFontSize(double fontSize)
    {
        double scale = fontSize / 14.0;
        App.Current.Resources["MarkdownBodyFontSize"] = fontSize;
        App.Current.Resources["MarkdownH1FontSize"] = 28.0 * scale;
        App.Current.Resources["MarkdownH2FontSize"] = 24.0 * scale;
        App.Current.Resources["MarkdownH3FontSize"] = 22.0 * scale;
        App.Current.Resources["MarkdownH4FontSize"] = 20.0 * scale;
        App.Current.Resources["MarkdownH5FontSize"] = 18.0 * scale;
        App.Current.Resources["MarkdownH6FontSize"] = 16.0 * scale;

        FontSize = fontSize;
    }
}
