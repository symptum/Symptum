namespace Symptum.Models;

public class SearchResult(
    SearchResultKind kind,
    string title,
    IReadOnlyList<string> breadcrumb,
    string relativePath,
    IReadOnlyList<SearchSnippet>? snippets = null)
{
    public SearchResultKind Kind { get; } = kind;

    public string Title { get; } = title;

    public IReadOnlyList<string> Breadcrumb { get; } = breadcrumb;

    public string RelativePath { get; } = relativePath;

    public IReadOnlyList<SearchSnippet> Snippets { get; } = snippets ?? [];
}
