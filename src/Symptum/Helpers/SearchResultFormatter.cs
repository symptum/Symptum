using Symptum.Models;
using Symptum.UI;

namespace Symptum.Helpers;

/// <summary>
/// Provides display helpers for search results used by the search page bindings.
/// </summary>
public static class SearchResultFormatter
{
    public static string GetGlyph(SearchResultKind kind) => kind switch
    {
        SearchResultKind.Package => CommonGlyphs.Package,   // Package
        SearchResultKind.Category => CommonGlyphs.List,  // List
        SearchResultKind.Markdown => "\uE8A5",  // Document
        SearchResultKind.Csv => CommonGlyphs.List,       // Diagnostic / table-ish
        SearchResultKind.Image => CommonGlyphs.Photo,     // Photo
        SearchResultKind.Audio => "\uE8D6",     // MusicInfo
        _ => "\uE7C3"                           // Page
    };

    public static string GetBreadcrumb(IReadOnlyList<string> breadcrumb) =>
        breadcrumb.Count == 0 ? string.Empty : string.Join(" > ", breadcrumb);
}
