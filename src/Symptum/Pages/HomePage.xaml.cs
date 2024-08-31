using Symptum.Navigation;

namespace Symptum.Pages;

public sealed partial class HomePage : NavigablePage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationManager.Navigate(new Uri("symptum://subjects/an"));
    }
}
