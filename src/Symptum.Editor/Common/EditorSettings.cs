using System.Text.Json;
using Symptum.Common.Helpers;

namespace Symptum.Editor.Common;

public static class EditorSettings
{
    private static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;
    private const string RecentItemsKey = "RecentItems";

    public static string? Author
    {
        get => AppDataHelper.GetValue(string.Empty);
        set => AppDataHelper.SetValue(value);
    }

    public static bool ReopenPreviousWorkFolder
    {
        get => AppDataHelper.GetValue(true);
        set => AppDataHelper.SetValue(value);
    }

    public static string? PreviousWorkFolderPath
    {
        get => AppDataHelper.GetValue(string.Empty);
        set => AppDataHelper.SetValue(value);
    }

    public static bool ShowResourcesPane
    {
        get => AppDataHelper.GetValue(true);
        set => AppDataHelper.SetValue(value);
    }

    public static bool ShowStatusBar
    {
        get => AppDataHelper.GetValue(true);
        set => AppDataHelper.SetValue(value);
    }

    public static bool ShowOutputPanel
    {
        get => AppDataHelper.GetValue(true);
        set => AppDataHelper.SetValue(value);
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
