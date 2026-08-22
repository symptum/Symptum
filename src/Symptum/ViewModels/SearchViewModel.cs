using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Models;
using Symptum.Navigation;
using System.Threading.Tasks;

namespace Symptum.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private const int DefaultMaxResults = 50;
    private const int DebounceDelayMs = 300;
    private const int MinQueryLength = 2;
    private static bool _initialized = false;

    private CancellationTokenSource? _cancellationTokenSource;

    public static SearchViewModel Instance { get; } = new();

    [ObservableProperty]
    public partial string? QueryText { get; set; }

    [ObservableProperty]
    public partial bool SearchContent { get; set; } = true;

    [ObservableProperty]
    public partial bool SearchMetadata { get; set; } = true;

    [ObservableProperty]
    public partial bool MatchCase { get; set; }

    [ObservableProperty]
    public partial PackageScopeOption? SelectedScope { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; private set; }

    [ObservableProperty]
    public partial string? StatusText { get; private set; }

    public ObservableCollection<SearchResult> Results { get; } = [];

    public ObservableCollection<PackageScopeOption> ScopeOptions { get; } = [];

    partial void OnQueryTextChanged(string value) => ScheduleSearchAsync();

    partial void OnSearchContentChanged(bool value) => ScheduleSearchAsync();

    partial void OnSearchMetadataChanged(bool value) => ScheduleSearchAsync();

    partial void OnMatchCaseChanged(bool value) => ScheduleSearchAsync();

    public void LoadScopeOptions()
    {
        if (_initialized) return;
        _initialized = true;
        PackageScopeOption? selected = SelectedScope;
        ScopeOptions.Add(new(null, "All packages"));
        foreach (IResource resource in ResourceManager.Resources)
        {
            if (resource is PackageResource package)
                ScopeOptions.Add(new(package.Title, package.Title));
        }
        SelectedScope = selected != null && ScopeOptions.Any(o => o.DisplayName == selected.DisplayName) ? selected : ScopeOptions[0];
    }

    public void CancelSearch() => _cancellationTokenSource?.Cancel();

    private async Task ScheduleSearchAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(QueryText) || QueryText.Trim().Length < MinQueryLength)
        {
            Results.Clear();
            IsSearching = false;
            StatusText = null;
            return;
        }

        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        try
        {
            IsSearching = true;
            await Task.Delay(DebounceDelayMs, cancellationToken);
            await RunSearchAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            if (_cancellationTokenSource?.Token == cancellationToken)
                IsSearching = false;
        }
    }

    public async Task SearchAsync()
    {
        _cancellationTokenSource ??= new CancellationTokenSource();
        try
        {
            IsSearching = true;
            await RunSearchAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task RunSearchAsync(CancellationToken cancellationToken)
    {
        SearchQuery query = new()
        {
            QueryText = QueryText,
            Locations = (SearchContent ? SearchLocations.Content : SearchLocations.None) |
                        (SearchMetadata ? SearchLocations.Metadata : SearchLocations.None),
            ScopePackage = SelectedScope?.Title,
            MatchCase = MatchCase,
            MaxResults = DefaultMaxResults
        };

        IReadOnlyList<SearchResult> results = await Task.Run(() => SearchHelper.SearchAsync(query, cancellationToken), cancellationToken);

        Results.Clear();
        foreach (SearchResult result in results)
        {
            Results.Add(result);
        }

        StatusText = results.Count == 0 ? $"No results for \"{query.QueryText}\"" : $"{results.Count} result(s)";
    }

    public async Task OpenResultAsync(SearchResult? result)
    {
        IResource? resource = await SearchHelper.LoadResultResourceAsync(result);
        NavigationManager.Navigate(resource as INavigable);
    }
}

public class PackageScopeOption(string? title, string displayName)
{
    public string? Title { get; } = title;

    public string DisplayName { get; } = displayName;

    public override string ToString() => DisplayName;
}
