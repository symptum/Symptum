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
        repeater.ItemsSource = (navigable as ReferenceValueGroup)?.Parameters ?? null;
    }

    protected override void OnLayoutChanged(bool isWide) => repeater.Margin = isWide switch
    {
        true => ContentMargin,
        false => NarrowContentMargin,
    };
}
