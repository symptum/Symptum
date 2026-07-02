using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Navigation;

namespace Symptum.Pages;

public sealed partial class ReferenceValueGroupPage : NavigablePage
{
    public ReferenceValueGroupPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigableChanged(INavigable? navigable)
    {
        parameters.ItemsSource = (navigable as ReferenceValueGroup)?.Parameters ?? null;
    }
}
