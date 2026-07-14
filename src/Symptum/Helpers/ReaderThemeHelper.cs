namespace Symptum.Helpers;

public class ReaderThemeHelper
{
    private static XamlRoot _root;

    public static readonly List<string> _themeNames =
    [
        // "Default",
        "Forest",
        // "Air",
        // "Water",
        // "Fire",
        // "Earth",
    ];

    private static readonly Dictionary<string, ResourceDictionary> _themes = [];

    static ReaderThemeHelper()
    {

    }

    public static List<string> ThemeNames { get => _themeNames; }

    public static void Initialize(XamlRoot root)
    {
        _root = root;
        foreach (var name in _themeNames)
        {
            ResourceDictionary res = new() { Source = new Uri($"ms-appx:///ReaderThemes/{name}.xaml") };
            _themes[name] = res;
        }
    }

    public static void ApplyTheme(string name)
    {
        RemoveThemes();

        if (_themes.TryGetValue(name, out var res))
            App.Current.Resources.MergedDictionaries.Add(res);
        
        ElementTheme current = (ElementTheme)SystemThemeHelper.GetRootTheme(_root);
        SystemThemeHelper.SetApplicationTheme(_root,
            current == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light);
        SystemThemeHelper.SetApplicationTheme(_root, current);
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
}
