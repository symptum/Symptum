namespace Symptum.Models;

public class SearchResult(
    SearchResultKind kind,
    string title,
    IReadOnlyList<string> breadcrumb,
    string relativePath)
{
    public SearchResultKind Kind { get; } = kind;

    public string Title { get; private set; } = title;

    public IReadOnlyList<string> Breadcrumb { get; } = breadcrumb;

    public string RelativePath { get; } = relativePath;

    public IReadOnlyList<SearchSegment> TitleSegments { get; private set; }
        = [new SearchSegment(title, false)];

    public bool HasTitleMatch { get; internal set; }

    public List<SearchMatch> Matches { get; } = [];

    public int TotalMatches { get; internal set; }

    public double Score { get; internal set; }

    public bool HasMatches => Matches.Count > 0;

    public bool HasSummary => Kind == SearchResultKind.Csv
        ? TotalMatches > 0
        : TotalMatches > Matches.Count;

    public string SummaryText
    {
        get
        {
            int extra = TotalMatches - Matches.Count;
            return Kind == SearchResultKind.Csv
                ? $"{TotalMatches} match{(TotalMatches == 1 ? string.Empty : "es")}"
                : $"+{extra} more match{(extra == 1 ? string.Empty : "es")}";
        }
    }

    internal void UpdateTitle(string title, bool matched, IReadOnlyList<SearchSegment>? segments = null)
    {
        Title = title;
        TitleSegments = segments is { Count: > 0 } ? segments : [new SearchSegment(title, false)];
        HasTitleMatch = matched;
    }
}
