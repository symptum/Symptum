using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Helpers;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Management.Resources;
using Symptum.Models;
using Symptum.Navigation;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI;

namespace Symptum.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private const int DefaultMaxResults = 50;
    private const int DebounceDelayMs = 300;
    private const int MinQueryLength = 2;
    private static bool _initialized = false;

    private CancellationTokenSource? _cancellationTokenSource;
    private string? _lastSearchText;
    private int _searchGeneration = 0;
    private readonly DispatcherQueue _uiDispatcher;

    public static SearchViewModel Instance { get; } = new();

    private SearchViewModel()
    {
        _uiDispatcher = DispatcherQueue.GetForCurrentThread();
    }

    [ObservableProperty]
    public partial string? QueryText { get; set; }

    [ObservableProperty]
    public partial bool SearchContent { get; set; } = true;

    [ObservableProperty]
    public partial bool SearchMetadata { get; set; } = true;

    [ObservableProperty]
    public partial bool MatchCase { get; set; }

    [ObservableProperty]
    public partial bool MatchWholeWord { get; set; }

    [ObservableProperty]
    public partial PackageScopeOption? SelectedScope { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; private set; }

    [ObservableProperty]
    public partial string? StatusText { get; private set; }

    [ObservableProperty]
    public partial string? NoResultsText { get; private set; }

    [ObservableProperty]
    public partial bool HasNoResults { get; private set; }

    public ObservableCollection<SearchResult> Results { get; } = [];

    public ObservableCollection<PackageScopeOption> ScopeOptions { get; } = [];

    partial void OnQueryTextChanged(string value) => ScheduleSearchAsync();

    partial void OnSearchContentChanged(bool value) => ScheduleSearchAsync();

    partial void OnSearchMetadataChanged(bool value) => ScheduleSearchAsync();

    partial void OnMatchCaseChanged(bool value) => ScheduleSearchAsync();

    partial void OnMatchWholeWordChanged(bool value) => ScheduleSearchAsync();

    partial void OnSelectedScopeChanged(PackageScopeOption? value) => ScheduleSearchAsync();

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

    public void CancelSearch()
    {
        _cancellationTokenSource?.Cancel();
        Interlocked.Increment(ref _searchGeneration);
    }

    private async Task ScheduleSearchAsync()
    {
        int currentGeneration = Interlocked.Increment(ref _searchGeneration);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(QueryText) || QueryText.Trim().Length < MinQueryLength)
        {
            await ClearResultsAsync();
            IsSearching = false;
            StatusText = null;
            NoResultsText = null;
            HasNoResults = false;
            return;
        }

        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        try
        {
            IsSearching = true;
            await Task.Delay(DebounceDelayMs, cancellationToken);

            if (currentGeneration != _searchGeneration || cancellationToken.IsCancellationRequested)
                return;

            await RunSearchAsync(cancellationToken, currentGeneration);
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
        int currentGeneration = Interlocked.Increment(ref _searchGeneration);

        _cancellationTokenSource?.Cancel();
        CancellationTokenSource cts = new();
        _cancellationTokenSource = cts;

        try
        {
            IsSearching = true;
            await RunSearchAsync(cts.Token, currentGeneration);
        }
        catch (OperationCanceledException)
        { }
        finally
        {
            if (_cancellationTokenSource == cts)
                IsSearching = false;
        }
    }

    public async Task SubmitAsync(string? queryText)
    {
        string trimmed = (queryText ?? string.Empty).Trim();

        if (trimmed.Length >= MinQueryLength &&
            string.Equals(_lastSearchText, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            if (Results.Count > 0)
                await OpenResultAsync(Results[0]);
            return;
        }

        QueryText = trimmed;
        await SearchAsync();
    }

    private async Task RunSearchAsync(CancellationToken cancellationToken, int searchGeneration)
    {
        string trimmed = QueryText?.Trim() ?? string.Empty;
        if (trimmed.Length < MinQueryLength)
        {
            await ClearResultsAsync();
            StatusText = null;
            NoResultsText = null;
            HasNoResults = false;
            _lastSearchText = null;
            return;
        }

        SearchQuery query = new()
        {
            QueryText = trimmed,
            Locations = (SearchContent ? SearchLocations.Content : SearchLocations.None) |
                        (SearchMetadata ? SearchLocations.Metadata : SearchLocations.None),
            ScopePackage = SelectedScope?.Title,
            MatchCase = MatchCase,
            MatchWholeWord = MatchWholeWord,
            MaxResults = DefaultMaxResults
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<SearchResult> results;
        try
        {
            results = await Task.Run(() => SearchHelper.SearchAsync(query, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        stopwatch.Stop();

        if (searchGeneration != _searchGeneration || cancellationToken.IsCancellationRequested)
            return;

        await _uiDispatcher.EnqueueAsync(() =>
        {
            if (Results.Count > 0) Results.Clear();
            foreach (var result in results) Results.Add(result);
        });

        _lastSearchText = trimmed;
        HasNoResults = results.Count == 0;
        NoResultsText = HasNoResults ? $"No results for \"{trimmed}\"" : null;
        StatusText = HasNoResults ? null :
            $"{results.Count} result{(results.Count == 1 ? string.Empty : "s")} in {stopwatch.ElapsedMilliseconds} ms";
        results.Clear();
    }

    private async Task ClearResultsAsync()
    {
        await _uiDispatcher.EnqueueAsync(() =>
        {
            // Directly calling Clear causes unhandled exception in WinUI.
            if (Results.Count > 0) Results.Clear();
        });
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
