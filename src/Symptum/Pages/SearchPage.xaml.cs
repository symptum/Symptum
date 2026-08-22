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
        searchBox.Focus(FocusState.Programmatic);
    }

    private void SearchPage_Unloaded(object sender, RoutedEventArgs e) => ViewModel.CancelSearch();

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            ViewModel.QueryText = sender.Text;
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.QueryText = args.QueryText;
        await ViewModel.SearchAsync();
    }

    private async void ResultsLV_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SearchResult result)
            await ViewModel.OpenResultAsync(result);
    }

    protected override void OnLayoutChanged(bool isWide) => rootGrid.Margin = isWide switch
    {
        true => ContentMargin,
        false => NarrowContentMargin
    };
}
