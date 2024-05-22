using Symptum.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Symptum.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SubjectsPage : Page
{
    public SubjectsPage()
    {
        this.InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationManager.GetNavigationManager().Navigate((sender as Button).Tag.ToString());
    }
}
