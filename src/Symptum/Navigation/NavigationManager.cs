using System.Collections.ObjectModel;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Pages;

namespace Symptum.Navigation;

public class NavigationManager
{
    public static readonly NavigationInfo HomeNavInfo = new(HomeUri, "Home", typeof(HomePage), new SymbolIconSource() { Symbol = Symbol.Home });

    public static readonly Uri HomeUri = ResourceManager.GetAbsoluteUri("home");

    public static INavigable? CurrentNavigable { get; set; }

    public static event EventHandler<INavigable> NavigationRequested;

    public static ObservableCollection<NavigationInfo> NavigationInfos { get; } = [ HomeNavInfo ];

    static NavigationManager()
    {
        NavigationInfo subjectNavInfo = new(ResourceManager.GetAbsoluteUri("subjects"), "Subjects", typeof(SubjectsPage), new SymbolIconSource() { Symbol = Symbol.Library });

        foreach (var sub in SubjectsManager.Subjects)
        {
            subjectNavInfo.Children.AddItemToListIfNotExists(CreateNavigationInfoForNavigable(sub));
        }

        NavigationInfos.Add(subjectNavInfo);
    }

    public static void Navigate(Uri? uri = null) => Navigate(GetNavigableForUri(uri));

    public static void Navigate(INavigable? navigable)
    {
        navigable ??= HomeNavInfo;
        NavigationRequested?.Invoke(null, navigable);
    }

    public static INavigable? GetNavigableForUri(Uri? uri)
    {
        INavigable? navigable = GetNavigationInfoForUri(uri);

        if (navigable == null && ResourceManager.TryGetResourceFromUri(uri, out var resource) && resource is INavigable navResource)
        {
            navigable = navResource;
        }

        return navigable;
    }

    public static NavigationInfo? GetNavigationInfoForUri(Uri? uri)
    {
        if (uri == null) return null;

        return FindNavigationInfo(navInfo => uri.Equals(navInfo.Uri));
    }

    private static NavigationInfo? FindNavigationInfo(Func<NavigationInfo, bool> predicate, IList<NavigationInfo>? collection = null)
    {
        collection ??= NavigationInfos;
        NavigationInfo? navInfo = null;

        foreach (var _navInfo in collection)
        {
            if (predicate(_navInfo))
            {
                navInfo = _navInfo;
            }
            else
                navInfo = FindNavigationInfo(predicate, _navInfo.Children);
            if (navInfo != null) return navInfo;
        }

        return null;
    }

    public static Type? GetPageTypeForUri(Uri? uri)
    {
        INavigable? navigable = GetNavigableForUri(uri);

        return GetPageTypeForNavigable(navigable);
    }

    public static Type? GetPageTypeForNavigable(INavigable? navigable)
    {
        if (navigable is NavigationInfo navInfo)
            return navInfo.PageType;
        else if (navigable is NavigableResource resource)
            return typeof(SubjectViewPage);
        else
            return null;
    }

    public static NavigationInfo? CreateNavigationInfoForNavigable(INavigable? navigable)
    {
        return navigable switch
        {
            Subject => new(navigable.Uri, navigable.Title, typeof(SubjectViewPage), new FontIconSource() { Glyph = "\uE82D" }),
            _ => null,
        };
    }
}
