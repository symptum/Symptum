using Symptum.Navigation;

namespace Symptum.Pages;

public sealed partial class SubjectsPage : NavigablePage
{
    public SubjectsPage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationManager.Navigate((sender as Button).Tag as Uri);
    }
}
