using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Pages;

namespace Symptum.Navigation;

public class NavigationManager
{
    private static readonly Dictionary<Uri, NavigationInfo> _navInfoMap = [];

    public static readonly NavigationInfo HomeNavInfo = new(HomeUri, "Home", typeof(HomePage), new SymbolIconSource() { Symbol = Symbol.Home });

    public static readonly Uri HomeUri = ResourceManager.GetAbsoluteUri("home");

    public static readonly Uri SubjectsUri = ResourceManager.GetAbsoluteUri("subjects");

    public static INavigable? CurrentNavigable { get; set; }

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

    public static Type? GetPageTypeForUri(Uri? uri)
    {
        INavigable? navigable = GetNavigableForUri(uri);

        return GetPageTypeForNavigable(navigable);
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
            Subject s => new(s, typeof(DefaultPage), new FontIconSource() { Glyph = "\uE82D" }),
            _ => null,
        };
    }

    private static void LoadNavigationInfosFromResources()
    {
        AddNavInfo(HomeNavInfo);
        NavigationInfo? navInfo;
        foreach (var resource in ResourceManager.Resources)
        {
            if (resource is Subject)
            {
                continue;
            }
            else if (resource is PackageResource package)
            {
                navInfo = new NavigationInfo(package.Uri, package.Title,
                    GetPageTypeForNavigable(package),
                    new FontIconSource() { Glyph = "\uE823" }, package);
                AddNavInfo(navInfo);
            }
        }

        navInfo = new NavigationInfo(SubjectsUri, "Subjects", typeof(DefaultPage), new SymbolIconSource() { Symbol = Symbol.Library });

        foreach (var sub in SubjectsManager.Subjects)
        {
            AddNavInfo(CreateNavigationInfoForNavigable(sub), navInfo.Children);
        }

        AddNavInfo(navInfo);
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
