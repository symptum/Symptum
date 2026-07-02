using System.Collections.ObjectModel;
using Symptum.Core.Management.Navigation;

namespace Symptum.Navigation;

// This will be used for data binding items to NavigationView

public class NavigationInfo : INavigable
{
    public NavigationInfo(NavigableResource navigableResource, Type? pageType, IconSource? iconSource = null)
    {
        Uri = navigableResource.Uri;
        Title = navigableResource.Title;
        PageType = pageType;
        IconSource = iconSource;
        BackingNavigable = navigableResource;
    }

    public NavigationInfo(Uri? uri, string? title, Type? pageType, IconSource? iconSource = null, INavigable? backingNavigable = null)
    {
        Uri = uri;
        Title = title;
        PageType = pageType;
        IconSource = iconSource;
        BackingNavigable = backingNavigable;
    }

    public Uri? Uri { get; set; }

    public string? Title { get; set; }

    public Type? PageType { get; set; }

    public IconSource? IconSource { get; set; }

    public ObservableCollection<NavigationInfo> Children { get; } = [];

    public INavigable? BackingNavigable { get; set; }
}
