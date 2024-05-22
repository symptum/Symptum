using System.Collections.ObjectModel;
using Symptum.Pages;
using Symptum.ViewModels;

namespace Symptum.Navigation;

internal class NavigationManager
{
    private static NavigationManager navigationManager;
    private NavigationInfo currentNavInfo = null;

    public NavigationInfo CurrentNavigationInfo { get => currentNavInfo; }

    public event EventHandler<NavigationRequestedEventArgs> NavigationRequested;

    private static ObservableCollection<NavigationInfo> navigationInfos = new()
        {
            new ("Home", "home", typeof(HomePage)),
            new ("Managements", "managements", typeof(ManagementsPage)),
        };

    public static ObservableCollection<NavigationInfo> NavigationInfos { get; } = navigationInfos;

    private NavigationManager()
    {
        NavigationInfo subjectNavInfo = new("Subjects", "subjects", typeof(SubjectsPage));

        foreach (var sub in MainViewModel.Subjects)
        {
            subjectNavInfo.Children.Add(new NavigationInfo(sub.Name, sub.Path, typeof(SubjectViewPage)));
        }

        navigationInfos.Add(subjectNavInfo);
    }

    public static void Initialize()
    {
        if (navigationManager == null)
            navigationManager = new();
    }

    public static NavigationManager GetNavigationManager()
    {
        if (navigationManager == null)
            throw new NullReferenceException($"{nameof(Initialize)} has not been called prior.");
        return navigationManager;
    }

    public void Navigate(string navPath)
    {
        currentNavInfo = GetNavigationInfoForPath(navPath);
        NavigationRequested?.Invoke(this, new NavigationRequestedEventArgs(navPath));
    }

    public void SetCurrentNavigationInfo(NavigationInfo navigationInfo)
    {
        currentNavInfo = navigationInfo;
    }


    public NavigationInfo GetNavigationInfoForPath(string navPath)
    {
        return FindNavigationInfo(navInfo => navInfo.Path.Equals(navPath));
    }

    private NavigationInfo FindNavigationInfo(Func<NavigationInfo, bool> func)
    {
        var navInfo = navigationInfos.FirstOrDefault(func);

        if (navInfo == null)
        {
            foreach (var _navInfo in navigationInfos)
            {
                navInfo = _navInfo.Children.FirstOrDefault(func);
            }
        }

        return navInfo;
    }

    public Type GetPageTypeForPath(string navPath)
    {
        return GetNavigationInfoForPath(navPath).PageType;
    }

    public NavigationInfo GetNavigationInfoForPageType(Type pageType)
    {
        return FindNavigationInfo(navInfo => navInfo.PageType == pageType);
    }

    public string GetPathForPageType(Type pageType)
    {
        return GetNavigationInfoForPageType(pageType).Path;
    }
}

public class NavigationInfo
{
    public NavigationInfo(string title, string path, Type pageType)
    {
        Title = title;
        Path = path;
        PageType = pageType;
    }

    public string Title { get; private set; }

    public Type PageType { get; private set; }

    public string Path { get; private set; }

    public ObservableCollection<NavigationInfo> Children { get; private set; } = new();
}

public class NavigationRequestedEventArgs : EventArgs
{
    public NavigationRequestedEventArgs(string path)
    {
        Path = path;
    }

    public string Path { get; private set; }
}
