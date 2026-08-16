using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Pages;
using Symptum.UI;

namespace Symptum.Navigation;

public class NavigationManager
{
    private static readonly Dictionary<Uri, NavigationInfo> _navInfoMap = [];

    public static readonly Uri HomeUri = ResourceManager.GetAbsoluteUri("home");

    public static readonly NavigationInfo HomeNavInfo = new(HomeUri, "Home", typeof(HomePage), IconHelper.HomeIconSource);

    public static readonly Uri SubjectsUri = ResourceManager.GetAbsoluteUri("subjects");

    public static readonly Uri FocusSessionUri = ResourceManager.GetAbsoluteUri("focus");

    public static readonly NavigationInfo FocusSessionNavInfo = new(FocusSessionUri, "Focus Session", typeof(FocusSessionPage), IconHelper.FocusSessionIconSource);

    public static readonly NavigationInfo SettingsNavInfo = new(ResourceManager.GetAbsoluteUri("settings"), "Settings", typeof(SettingsPage), IconHelper.SettingsIconSource);

    public static Uri? CurrentUri { get; set; }

    public static event EventHandler<INavigable>? NavigationRequested;

    public static ObservableCollection<NavigationInfo> NavigationInfos { get; } = [];

    static NavigationManager()
    { }

    public static void Initialize()
    {
        LoadNavigationInfosFromResources();
    }

    public static void Navigate(Uri? uri = null) => Navigate(GetNavigableForUri(uri));

    public static void Navigate(INavigable? navigable)
    {
        navigable ??= HomeNavInfo;
        NavigationRequested?.Invoke(null, navigable!);
    }

    public static INavigable? GetNavigableForUri(Uri? uri)
    {
        if (uri == null) return null;

        // Remember to implement query support in the navigation logic later.
        uri = new UriBuilder(uri) { Query = null }.Uri; // Strip the query text from the uri to get the resource.

        INavigable? navigable = GetNavigationInfoForUri(uri);

        if (navigable == null && ResourceManager.TryGetResourceByUri(uri, out var resource) && resource is INavigable navResource)
        {
            navigable = navResource;
        }

        return navigable;
    }

    public static NavigationInfo? GetNavigationInfoForUri(Uri? uri)
    {
        if (uri != null && _navInfoMap.TryGetValue(uri, out NavigationInfo? navInfo))
            return navInfo;

        return null;
    }

    public static Type? GetPageTypeForNavigable(INavigable? navigable)
    {
        return navigable switch
        {
            NavigationInfo n => n.PageType,
            ReferenceValueGroup => typeof(ReferenceValueGroupPage),
            MarkdownFileResource => typeof(MarkdownPage),
            ImageFileResource => typeof(ImagePage),
            NavigableResource => typeof(DefaultPage),
            _ => null,
        };
    }

    public static INavigable? GetRealNavigable(INavigable? navigable)
    {
        switch (navigable)
        {
            case NavigableResource resource:
                return resource;
            case NavigationInfo navInfo:
                {
                    return navInfo.BackingNavigable is NavigableResource res ? res : navInfo;
                }

            default:
                return null;
        }
    }

    public static NavigationInfo? CreateNavigationInfoForNavigable(INavigable? navigable)
    {
        return navigable switch
        {
            Subject s => new(s, typeof(DefaultPage), IconHelper.SubjectIconSource),
            _ => null,
        };
    }

    private static void LoadNavigationInfosFromResources()
    {
        AddNavInfo(HomeNavInfo);
        NavigationInfo? navInfo;
        navInfo = new NavigationInfo(SubjectsUri, "Subjects", typeof(DefaultPage), IconHelper.SubjectsLibraryIconSource);

        foreach (var sub in SubjectsManager.Subjects)
        {
            AddNavInfo(CreateNavigationInfoForNavigable(sub), navInfo.Children);
        }

        AddNavInfo(navInfo);

        foreach (var resource in ResourceManager.Resources)
        {
            if (resource is Subject)
            {
                continue;
            }
            else if (resource is PackageResource package)
            {
                var icon = IconHelper.GetIconSourceForUri(package.Uri);
                navInfo = new NavigationInfo(package.Uri, package.Title,
                    GetPageTypeForNavigable(package),
                    icon, package);
                AddNavInfo(navInfo);
            }
        }

        AddNavInfo(FocusSessionNavInfo);

        Navigate(HomeNavInfo);
    }

    private static void AddNavInfo(NavigationInfo? navInfo, ObservableCollection<NavigationInfo>? destination = null)
    {
        if (navInfo != null && navInfo.Uri != null)
        {
            _navInfoMap[navInfo.Uri] = navInfo;
            if (destination == null) NavigationInfos.Add(navInfo);
            else destination.Add(navInfo);
        }
    }
}
