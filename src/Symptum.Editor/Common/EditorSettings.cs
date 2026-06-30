using System.Text.Json;

namespace Symptum.Editor.Common;

public static class EditorSettings
{
    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

    private const string AuthorKey = nameof(Author);
    private const string LastWorkFolderPathKey = nameof(LastWorkFolderPath);
    private const string ShowResourcesPaneKey = nameof(ShowResourcesPane);
    private const string ShowStatusBarKey = nameof(ShowStatusBar);
    private const string ShowOutputPanelKey = nameof(ShowOutputPanel);
    private const string RecentItemsKey = "RecentItems";

    public static string? Author
    {
        get => LocalSettings.Values[AuthorKey] as string;
        set => LocalSettings.Values[AuthorKey] = value;
    }

    public static string? LastWorkFolderPath
    {
        get => LocalSettings.Values[LastWorkFolderPathKey] as string;
        set => LocalSettings.Values[LastWorkFolderPathKey] = value;
    }

    public static bool ShowResourcesPane
    {
        get => (bool)(LocalSettings.Values[ShowResourcesPaneKey] ?? true);
        set => LocalSettings.Values[ShowResourcesPaneKey] = value;
    }

    public static bool ShowStatusBar
    {
        get => (bool)(LocalSettings.Values[ShowStatusBarKey] ?? true);
        set => LocalSettings.Values[ShowStatusBarKey] = value;
    }

    public static bool ShowOutputPanel
    {
        get => (bool)(LocalSettings.Values[ShowOutputPanelKey] ?? true);
        set => LocalSettings.Values[ShowOutputPanelKey] = value;
    }

    public static void LoadRecentItems(IList<string>? source)
    {
        if (source == null || LocalSettings.Values[RecentItemsKey] is not string json) return;
        
        source.AddRange(JsonSerializer.Deserialize<List<string>>(json) ?? []);
    }

    public static void SaveRecentItems(IList<string>? source)
    {
        if (source == null) return;
        LocalSettings.Values[RecentItemsKey] = JsonSerializer.Serialize(source);
    }

    public static void ClearAllRecentItems() => LocalSettings.Values.Remove(RecentItemsKey);
}
