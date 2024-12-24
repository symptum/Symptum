using Symptum.Core.Management.Navigation;
using Symptum.Core.Subjects;
using Symptum.Navigation;

namespace Symptum.Pages;

public sealed partial class SubjectViewPage : NavigablePage
{
    public SubjectViewPage()
    {
        InitializeComponent();
    }

    #region Properties

    public static readonly DependencyProperty SubjectProperty = DependencyProperty.Register(
        nameof(Subject),
        typeof(Subject),
        typeof(SubjectViewPage),
        new(null));

    public Subject Subject
    {
        get => (Subject)GetValue(SubjectProperty);
        set => SetValue(SubjectProperty, value);
    }

    #endregion

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        if (navigable is NavigationInfo navInfo && navInfo.BackingNavigable is Subject subject)
        {
            Subject = subject;
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        NavigationManager.Navigate((sender as Button).Tag as INavigable);
    }
}
