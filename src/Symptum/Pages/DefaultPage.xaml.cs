using Symptum.Core.Management.Navigation;
using Symptum.Core.Subjects;
using Symptum.Navigation;

namespace Symptum.Pages;

public sealed partial class DefaultPage : NavigablePage
{
    public DefaultPage()
    {
        InitializeComponent();
    }
    protected override void OnNavigableChanged(INavigable? navigable)
    {
        repeater.ItemsSource = null;
        
        if (navigable is NavigableResource resource)
        {
            repeater.ItemsSource = resource.ChildrenResources;
        }
        else if (navigable is NavigationInfo n &&
            n.Uri == NavigationManager.SubjectsUri)
        {
            repeater.ItemsSource = SubjectsManager.Subjects;
        }
    }
}
