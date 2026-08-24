using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Symptum.Common.Helpers;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Models;
using Symptum.UI;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Helpers;

public static partial class SearchHelper
{
    private const string Ellipsis = "…";

    private sealed class CachedFile(string relativePath, StorageFile file)
    {
        public readonly string RelativePath = relativePath;
        public readonly StorageFile File = file;
        public IReadOnlyList<string>? Segments;
        public SearchResultKind Kind;
        public string Title = string.Empty;
    }

    private const double WeightTitle = 1000;
    private const double WeightPackageMetadata = 400;
    private const double WeightItemMetadata = 300;
    private const double WeightContent = 60;
    private const double WeightCsvEntry = 30;
    private const int SnippetRadius = 80;

    private static readonly object _cacheLock = new();
    private static List<CachedFile>? _cachedFiles;

    static SearchHelper()
    {
        ResourceHelper.WorkFolderChanged += (_, _) => InvalidateCache();
        ResourceHelper.WorkFolderFilesChanged += (_, _) => InvalidateCache();
    }

    public static void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedFiles?.Clear();
            _cachedFiles = null;
        }
    }

    #region File Enumeraton

    private static async Task<List<CachedFile>> GetTargetFilesAsync(string? scopePackage, CancellationToken cancellationToken)
    {
        StorageFolder? workFolder = ResourceHelper.WorkFolder;
        if (workFolder == null) return [];

        List<CachedFile> allFiles = await EnsureCacheAsync(workFolder, cancellationToken);

        return scopePackage == null ? allFiles
            : allFiles.Where(file =>
            {
                EnsureFileDetails(file);
                return file.Segments?.Count > 1 &&
                    string.Equals(file.Segments[0], scopePackage);
            }).ToList();
    }

    private static async Task<List<CachedFile>> EnsureCacheAsync(StorageFolder workFolder, CancellationToken cancellationToken)
    {
        lock (_cacheLock)
        {
            if (_cachedFiles != null) return _cachedFiles;
        }

        List<CachedFile> files = [];
        await EnumerateAsync(workFolder, PathSeparator.ToString(), files, cancellationToken);

        lock (_cacheLock)
        {
            _cachedFiles ??= files;
            return _cachedFiles;
        }
    }

    private static async Task EnumerateAsync(StorageFolder folder, string prefix, List<CachedFile> files, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (StorageFile file in await folder.GetFilesAsync())
            files.Add(new CachedFile(prefix + file.Name, file));

        foreach (StorageFolder subFolder in await folder.GetFoldersAsync())
            await EnumerateAsync(subFolder, prefix + subFolder.Name + PathSeparator, files, cancellationToken);
    }

    #endregion

    #region Query Terms

    [GeneratedRegex("\"(?<phrase>[^\"]+)\"|(?<word>[^\\s]+)", RegexOptions.Compiled)]
    private static partial Regex termRegex();

    private static string[] SplitTerms(string queryText)
    {
        List<string> terms = [];
        foreach (Match match in termRegex().Matches(queryText))
        {
            string term = (match.Groups["phrase"].Success ? match.Groups["phrase"].Value : match.Groups["word"].Value)
                .Trim().Trim('"').Trim();
            if (term.Length > 0 && !terms.Any(t => t.Equals(term, StringComparison.OrdinalIgnoreCase)))
                terms.Add(term);
        }
        return [.. terms];
    }

    private static List<(int Start, int Length)> FindRanges(string? text, string term, SearchQuery query, int searchStart = 0, int searchEnd = 0)
    {
        List<(int, int)> ranges = [];
        if (string.IsNullOrEmpty(text)) return ranges;

        List<int> indices = [];
        text.SearchTextAndFindAllMatches(ref indices, term,
            searchStart, searchEnd > searchStart ? searchEnd : 0,
            matchCase: query.MatchCase, matchWholeWord: query.MatchWholeWord);
        foreach (int index in indices) ranges.Add((index, term.Length));
        return ranges;
    }

    #endregion

    #region Search

    private static readonly Dictionary<string, SearchResult> _results = [];

    public static async Task<List<SearchResult>> SearchAsync(SearchQuery? query, CancellationToken cancellationToken = default)
    {
        _results.Clear();

        if (query == null || string.IsNullOrWhiteSpace(query.QueryText)) return [];

        string[] terms = SplitTerms(query.QueryText);
        if (terms.Length == 0) return [];

        int maxResults = query.MaxResults;

        List<CachedFile> files = await GetTargetFilesAsync(query.ScopePackage, cancellationToken);

        bool searchContent = query.Locations.HasFlag(SearchLocations.Content);
        bool searchMetadata = query.Locations.HasFlag(SearchLocations.Metadata);

        foreach (CachedFile file in files)
        {
            if (_results.Count > maxResults) break; // Temp
            cancellationToken.ThrowIfCancellationRequested();
            EnsureFileDetails(file);
            await SearchFileAsync(file, terms, query, searchContent, searchMetadata, cancellationToken);
        }

        List<SearchResult> finalResults = [.. _results.Values];
        finalResults.Sort(static (a, b) =>
        {
            int byScore = b.Score.CompareTo(a.Score);
            return byScore != 0 ? byScore : string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
        });
        _results.Clear();

        return finalResults.Count > maxResults ? [.. finalResults.Take(maxResults)] : finalResults;
    }

    private static async Task SearchFileAsync(CachedFile file, string[] terms, SearchQuery query,
        bool searchContent, bool searchMetadata, CancellationToken cancellationToken)
    {
        SearchResult? result = null;
        if (MatchTitle(file.Title, terms, query, out var titleSegments))
        {
            // If the title has matched we will create a result here and pass it content searching.
            result = GetOrCreateResult(file);
            result.Score += WeightTitle;
            result.UpdateTitle(file.Title, true, titleSegments);
        }

        if (!searchContent && !searchMetadata) return;

        try
        {
            switch (file.Kind)
            {
                case SearchResultKind.Markdown when searchContent:
                    await SearchMarkdownFileAsync(file, terms, query, result, cancellationToken);
                    break;
                case SearchResultKind.Csv when searchContent:
                    await SearchCsvFileAsync(file, terms, query, result, cancellationToken);
                    break;
                case SearchResultKind.Metadata when searchMetadata:
                    await SearchJsonFileAsync(file, terms, query, result, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static async Task SearchMarkdownFileAsync(CachedFile file, string[] terms, SearchQuery query, SearchResult? result, CancellationToken ct)
    {
        string text = await FileIO.ReadTextAsync(file.File);
        ct.ThrowIfCancellationRequested();

        // Quick reject: all terms must be present somewhere
        foreach (string term in terms)
            if (!text.Contains(term, query.MatchCase, query.MatchWholeWord)) return;

        int lineStart = 0;
        while (lineStart < text.Length)
        {
            ct.ThrowIfCancellationRequested();

            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0) lineEnd = text.Length;
            string line = text[lineStart..lineEnd].TrimEnd('\r');
            lineStart = lineEnd + 1;

            List<(int Start, int Length)> ranges = [];
            foreach (string term in terms)
                ranges.AddRange(FindRanges(line, term, query));
            if (ranges.Count == 0) continue;

            var match = CreateMatch(string.Empty, line, ranges);
            if (match != null)
            {
                result ??= GetOrCreateResult(file);
                result.Score += WeightContent;
                result.TotalMatches += ranges.Count;
                result.Matches.Add(match);
            }
            ranges.Clear();
        }
    }

    private static async Task SearchCsvFileAsync(CachedFile file, string[] terms, SearchQuery query, SearchResult? result, CancellationToken ct)
    {
        string csv = await FileIO.ReadTextAsync(file.File);
        ct.ThrowIfCancellationRequested();

        List<(int Start, int Length)> ranges = [];
        foreach (string term in terms)
            ranges.AddRange(FindRanges(csv, term, query));

        if (ranges.Count > 0)
        {
            result ??= GetOrCreateResult(file);
            result.Score += WeightCsvEntry;
            result.TotalMatches = ranges.Count;
        }
        ranges.Clear();
    }

    private static async Task SearchJsonFileAsync(CachedFile file, string[] terms, SearchQuery query, SearchResult? result, CancellationToken ct)
    {
        string json = await FileIO.ReadTextAsync(file.File);
        ct.ThrowIfCancellationRequested();

        JsonDocument document;
        try { document = JsonDocument.Parse(json); }
        catch { return; }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object) return;

            // Only PackageResource has Description and Tags property.
            // MetadataResource may contain the properties of its children (FileResource).
            // PackageResource uses $feature discriminator. While MetadataResource uses $type.
            bool isPackage = document.RootElement.TryGetProperty("$feature", out _);
            if (isPackage)
            {
                ScanJsonObject(document.RootElement, file.RelativePath, terms, query, file, result);
            }

            // Scan nested items in Contents, Items, Documents, Images arrays
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if ((property.Name == "Contents" || property.Name == "Items" ||
                     property.Name == "Documents" || property.Name == "Images")
                    && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            ScanJsonObject(item, file.RelativePath, terms, query);
                        }
                    }
                }
            }
        }
    }

    private static void ScanJsonObject(JsonElement obj, string jsonFilePath, string[] terms, SearchQuery query, CachedFile? file = null, SearchResult? result = null)
    {
        string? title = null;
        string? description = null;
        List<string> tags = [];
        string? filePath = null;

        foreach (JsonProperty property in obj.EnumerateObject())
        {
            switch (property.Name)
            {
                case "Title" when property.Value.ValueKind == JsonValueKind.String:
                    title = property.Value.GetString();
                    break;
                case "Description" when property.Value.ValueKind == JsonValueKind.String:
                    description = property.Value.GetString();
                    break;
                case "Tags" when property.Value.ValueKind == JsonValueKind.Array:
                    foreach (var tag in property.Value.EnumerateArray())
                        if (tag.ValueKind == JsonValueKind.String)
                            tags.Add(tag.GetString()!);
                    break;
                case "FilePath" when property.Value.ValueKind == JsonValueKind.String:
                    filePath = property.Value.GetString();
                    break;
            }
        }

        // Build all matchable text from Description and Tags
        List<string> searchableValues = [];
        if (!string.IsNullOrEmpty(description)) searchableValues.Add(description);
        searchableValues.AddRange(tags);

        if (searchableValues.Count == 0) return;

        // Check if any term matches across Description + Tags (OR per term, AND across terms)
        int requiredMask = (1 << terms.Length) - 1;
        int foundMask = 0;
        List<SearchMatch> matches = [];
        int totalMatches = 0;

        for (int i = 0; i < searchableValues.Count; i++)
        {
            string value = searchableValues[i];
            int valueMask = 0;
            List<(int Start, int Length)> ranges = [];
            for (int j = 0; j < terms.Length; j++)
            {
                var termRanges = FindRanges(value, terms[j], query);
                if (termRanges.Count > 0)
                {
                    valueMask |= 1 << j;
                    ranges.AddRange(termRanges);
                    termRanges.Clear();
                }
            }
            if (valueMask != 0)
            {
                foundMask |= valueMask;
                totalMatches += ranges.Count;
                bool isDescription = i == 0 && !string.IsNullOrEmpty(description);
                var match = CreateMatch(!isDescription ? "Tags" : "Description", value, ranges);
                if (match != null) matches.Add(match);
            }
        }

        searchableValues.Clear();

        // All terms must be found (AND logic)
        if ((foundMask & requiredMask) != requiredMask) return;

        // A package
        if (file != null)
        {
            result ??= GetOrCreateResult(file);
            result.Score += WeightPackageMetadata;
            result.TotalMatches += totalMatches;
            result.Matches.AddRange(matches);
        }
        // A file metadata
        else if (filePath != null)
        {
            if (!_results.TryGetValue(filePath, out SearchResult? r))
            {
                (string folder, string fileName, string extension) = GetDetailsFromFilePath(filePath);
                var breadcrumb = folder.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                var kind = GetKindFromExtension(extension);
                r = CreateResult(filePath, kind, title ?? fileName, breadcrumb);
            }
            r.Score += WeightItemMetadata;
            r.TotalMatches += totalMatches;
            r.Matches.AddRange(matches);
        }

        matches.Clear();
    }

    #endregion

    #region Helper Methods

    private static SearchResult GetOrCreateResult(CachedFile file)
    {
        if (!_results.TryGetValue(file.RelativePath, out SearchResult? result))
        {
            EnsureFileDetails(file);
            result = CreateResult(file.RelativePath, file.Kind, file.Title, file.Segments ?? []);
        }
        return result;
    }

    private static SearchResult CreateResult(string relativePath, SearchResultKind kind, string title, IReadOnlyList<string> breadcrumb)
    {
        var result = new SearchResult(kind, title, breadcrumb, relativePath);
        _results[relativePath] = result;
        return result;
    }

    private static bool MatchTitle(string title, string[] terms, SearchQuery query, out IReadOnlyList<SearchSegment>? segments)
    {
        List<(int Start, int Length)> ranges = [];
        int mask = 0;
        for (int i = 0; i < terms.Length; i++)
        {
            var termRanges = FindRanges(title, terms[i], query);
            if (termRanges.Count > 0)
            {
                mask |= 1 << i;
                ranges.AddRange(termRanges);
            }
        }

        bool matched = mask == (1 << terms.Length) - 1 && ranges.Count > 0;
        if (matched)
            segments = BuildSegments(title, 0, title.Length, ranges);
        else
            segments = null;

        return matched;
    }

    private static SearchMatch? CreateMatch(string field, string text, List<(int Start, int Length)> ranges)
    {
        if (ranges.Count == 0) return null;

        int firstStart = ranges.Min(r => r.Start);
        int lastEnd = ranges.Max(r => r.Start + r.Length);

        int start = Math.Max(0, firstStart - SnippetRadius);
        int end = Math.Min(text.Length, lastEnd + SnippetRadius);

        List<SearchSegment> segments = BuildSegments(text, start, end, ranges);

        if (start > 0) segments.Insert(0, new SearchSegment(Ellipsis, false));
        if (end < text.Length) segments.Add(new SearchSegment(Ellipsis, false));

        if (segments.Count == 0) return null;
        return new SearchMatch(field, segments);
    }

    private static List<SearchSegment> BuildSegments(string text, int start, int end, IEnumerable<(int Start, int Length)> ranges)
    {
        List<(int S, int E)> merged = MergeRanges(ranges, start, end);
        if (merged.Count == 0) return [];

        StringBuilder sb = new(end - start);
        List<int> offsets = [];
        bool pendingSpace = false;
        for (int i = start; i < end; i++)
        {
            char ch = text[i];
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                sb.Append(' ');
                offsets.Add(-1);
                pendingSpace = false;
            }
            sb.Append(ch);
            offsets.Add(i);
        }

        int length = sb.Length;
        if (length == 0) return [];

        bool[] isMatch = new bool[length];
        foreach ((int s, int e) in merged)
        {
            for (int p = 0; p < length; p++)
            {
                int offset = offsets[p];
                if (offset >= s && offset < e) isMatch[p] = true;
            }
        }

        List<SearchSegment> segments = [];
        int segmentStart = 0;
        for (int p = 1; p <= length; p++)
        {
            if (p < length && isMatch[p] == isMatch[p - 1]) continue;
            segments.Add(new(sb.ToString(segmentStart, p - segmentStart), isMatch[p - 1]));
            segmentStart = p;
        }
        return segments;
    }

    private static List<(int S, int E)> MergeRanges(IEnumerable<(int Start, int Length)> ranges, int clipStart, int clipEnd)
    {
        var clipped = ranges
            .Select(r => (S: Math.Max(clipStart, r.Start), E: Math.Min(clipEnd, r.Start + r.Length)))
            .Where(r => r.E > r.S)
            .OrderBy(r => r.S)
            .ToList();

        List<(int S, int E)> merged = [];
        foreach ((int s, int e) in clipped)
        {
            if (merged.Count > 0 && s <= merged[^1].E)
            {
                if (e > merged[^1].E) merged[^1] = (merged[^1].S, e);
            }
            else merged.Add((s, e));
        }
        return merged;
    }

    private static void EnsureFileDetails(CachedFile file)
    {
        if (file.Segments != null) return;

        (string folder, string fileName, string extension) = GetDetailsFromFilePath(file.RelativePath);

        file.Segments = folder.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        file.Kind = GetKindFromExtension(extension);
        file.Title = fileName;
    }

    public static SearchResultKind GetKindFromExtension(string? extension) => extension?.ToLowerInvariant() switch
    {
        MarkdownFileExtension => SearchResultKind.Markdown,
        CsvFileExtension => SearchResultKind.Csv,
        JsonFileExtension => SearchResultKind.Metadata,
        JpegFileExtension or JpgFileExtension or PngFileExtension or BmpFileExtension or SvgFileExtension => SearchResultKind.Image,
        Mp3FileExtension => SearchResultKind.Audio,
        _ => SearchResultKind.Unknown
    };

    #endregion

    #region Opening Results

    public static async Task<IResource?> LoadResultResourceAsync(SearchResult? result)
    {
        if (result == null) return null;

        var segments = GetSegmentsFromPath(result.RelativePath);
        if (segments.Count == 0) return null;

        PackageResource? root = FindRootPackage(segments[0]);
        if (root == null) return null;

        if (!root.HasInitialized)
            await ResourceHelper.LoadResourceAsync(root);

        IResource current = root;
        foreach (string segment in segments.Skip(1))
        {
            IResource? child = await FindChildAsync(current, segment);
            if (child == null) return null;

            await ResourceHelper.LoadResourceAsync(child, current);
            current = child;
        }

        return current;
    }

    private static PackageResource? FindRootPackage(string title)
    {
        foreach (IResource resource in ResourceManager.Resources)
        {
            if (resource is PackageResource package &&
                string.Equals(package.Title, title))
                return package;
        }
        return null;
    }

    private static async Task<IResource?> FindChildAsync(IResource parent, string title)
    {
        if (!parent.HasInitialized)
            await ResourceHelper.LoadResourceAsync(parent, parent.ParentResource);

        return FindChildByTitle(parent, title);
    }

    private static IResource? FindChildByTitle(IResource parent, string title)
    {
        foreach (IResource child in parent.ChildrenResources ?? [])
        {
            if (string.Equals(child.Title, title))
                return child;

            (_, string fileName, _) = GetDetailsFromFilePath(
                (child as IMetadataResource)?.MetadataPath);

            if (string.Equals(fileName, title))
                return child;
        }
        return null;
    }

    private static List<string> GetSegmentsFromPath(string path)
    {
        List<string> results = [];
        (string folder, string fileName, _) = GetDetailsFromFilePath(path);
        results.AddRange(folder.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        results.Add(fileName);
        return results;
    }

    #endregion

    #region Result Formatting

    public static string GetGlyph(SearchResultKind kind) => kind switch
    {
        SearchResultKind.Markdown => CommonGlyphs.Document,
        SearchResultKind.Csv => CommonGlyphs.List,
        SearchResultKind.Image => CommonGlyphs.Photo,
        SearchResultKind.Audio => CommonGlyphs.Audio,
        _ => CommonGlyphs.GroupList
    };

    public static string GetBreadcrumb(IReadOnlyList<string> breadcrumb) =>
        breadcrumb.Count == 0 ? string.Empty : string.Join(" > ", breadcrumb);

    public static Visibility GetBreadcrumbVisibility(IReadOnlyList<string> breadcrumb) =>
        breadcrumb.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    private static Brush? _matchBrush;

    public static readonly DependencyProperty SegmentsProperty =
        DependencyProperty.RegisterAttached("Segments", typeof(IReadOnlyList<SearchSegment>),
            typeof(SearchHelper), new PropertyMetadata(null, OnSegmentsChanged));

    public static void SetSegments(TextBlock element, IReadOnlyList<SearchSegment>? value) =>
        element.SetValue(SegmentsProperty, value);

    public static IReadOnlyList<SearchSegment>? GetSegments(TextBlock element) =>
        element.GetValue(SegmentsProperty) as IReadOnlyList<SearchSegment>;

    private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        Apply(textBlock, e.NewValue as IReadOnlyList<SearchSegment>);
    }

    private static void Apply(TextBlock textBlock, IReadOnlyList<SearchSegment>? segments)
    {
        textBlock.Inlines.Clear();
        if (segments == null) return;

        foreach (SearchSegment segment in segments)
        {
            Run run = new() { Text = segment.Text };
            if (segment.IsMatch)
            {
                run.FontWeight = FontWeights.Bold;
                run.Foreground = GetMatchBrush();
            }
            textBlock.Inlines.Add(run);
        }
    }

    private static Brush? GetMatchBrush()
    {
        if (_matchBrush == null &&
            Application.Current.Resources.TryGetValue("AccentTextFillColorPrimaryBrush", out object? value) &&
            value is Brush brush)
        {
            _matchBrush = brush;
        }
        return _matchBrush;
    }

    #endregion
}
