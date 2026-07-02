using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;

namespace Symptum.Pages;

public sealed partial class HomePage : NavigablePage
{
    public HomePage()
    {
        InitializeComponent();
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        favorites.ItemsSource = ResourceManager.Resources
            .Where(r => r is PackageResource && r is not Subject);
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        favorites.ItemsSource = null;
    }
}
