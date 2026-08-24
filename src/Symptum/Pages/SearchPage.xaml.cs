using Microsoft.UI.Xaml.Input;
using Symptum.Models;
using Symptum.ViewModels;

namespace Symptum.Pages;

public sealed partial class SearchPage : NavigablePage
{
    public SearchPage()
    {
        InitializeComponent();
        Loaded += SearchPage_Loaded;
        Unloaded += SearchPage_Unloaded;
    }

    public SearchViewModel ViewModel => SearchViewModel.Instance;

    private void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadScopeOptions();
        FocusSearchBox();
    }

    private void SearchPage_Unloaded(object sender, RoutedEventArgs e) => ViewModel.CancelSearch();

    private void FocusSearchBox()
    {
        searchBox.Focus(FocusState.Programmatic);
    }

    private void FocusSearchAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        FocusSearchBox();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            ViewModel.QueryText = sender.Text;
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await ViewModel.SubmitAsync(args.QueryText);
    }

    protected override void OnLayoutChanged(bool isWide) => rootGrid.Margin = isWide switch
    {
        true => ContentMargin,
        false => NarrowContentMargin
    };

    private async void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is SearchResult result)
            await ViewModel.OpenResultAsync(result);
    }
}
