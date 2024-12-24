using System.Collections.ObjectModel;
using Symptum.Core.Management.Navigation;

namespace Symptum.Navigation;

// This will be used for data binding items to NavigationView

public class NavigationInfo(Uri? uri, string? title, Type? pageType, IconSource? iconSource = null, INavigable? backingNavigable = null) : INavigable
{
    public Uri? Uri { get; set; } = uri;

    public string? Title { get; set; } = title;

    public Type? PageType { get; set; } = pageType;

    public IconSource? IconSource { get; set; } = iconSource;

    public ObservableCollection<NavigationInfo> Children { get; } = [];

    public INavigable BackingNavigable { get; set; } = backingNavigable;
}
