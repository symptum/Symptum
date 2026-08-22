using System.Collections.ObjectModel;
using Symptum.Common.Helpers;
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

    public static readonly Uri SearchUri = ResourceManager.GetAbsoluteUri("search");

    public static readonly NavigationInfo SearchNavInfo = new(SearchUri, "Search", typeof(SearchPage), IconHelper.SearchIconSource);

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

    public static async Task NavigateAsync(Uri? uri = null)
    {
        if (uri == null) return;

        // Remember to implement query support in the navigation logic later.
        uri = StripQuery(uri); // Strip the query text from the uri to get the resource.

        INavigable? navigable = await GetNavigableForUriAsync(uri);
        Navigate(navigable);
    }

    public static void Navigate(INavigable? navigable)
    {
        navigable ??= HomeNavInfo;
        NavigationRequested?.Invoke(null, navigable!);
    }

    private static Uri StripQuery(Uri uri) => new UriBuilder(uri) { Query = null }.Uri;

    public static async Task<INavigable?> GetNavigableForUriAsync(Uri uri)
    {
        INavigable? navigable = GetNavigationInfoForUri(uri);
        if (navigable != null) return navigable;

        IResource? resource = await LoadResourceTreeAsync(uri);

        if (resource is INavigable navResource)
            return navResource;

        return null;
    }

    public static async Task<IResource?> LoadResourceTreeAsync(Uri? uri)
    {
        bool _resourceLoaded = false;
        IResource? resource = null;
        while (!_resourceLoaded)
        {
            // Load the children first.
            if (resource?.ChildrenResources != null)
                await ResourceHelper.LoadChildrenAsync(resource);

            if (ResourceManager.TryGetAvailableChildResourceByUri(uri,
                // Then scope to children
                resource?.ChildrenResources ?? ResourceManager.Resources, out resource))
            {
                // The exact resource has been found.
                // Ensure it has also been loaded.
                await ResourceHelper.LoadResourceAsync(resource);
                _resourceLoaded = true;
                break;
            }
            // We couldn't find any matching resource or its parent.
            if (resource == null) break;
        }
        return resource;
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
        AddNavInfo(SearchNavInfo);
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
