using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Common.Helpers;
using Symptum.Models;
using System.Text;
using System.Text.Json;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Helpers;

public static class SearchHelper
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

    private const int MaxSnippetsPerFile = 3;

    private static readonly object _cacheLock = new();

    private static List<CachedFile>? _cachedFiles;

    private static async Task<List<CachedFile>> GetTargetFilesAsync(string? scopePackage, CancellationToken cancellationToken)
    {
        StorageFolder? workFolder = ResourceHelper.WorkFolder;
        if (workFolder == null) return [];

        List<CachedFile> allFiles = await EnsureCacheAsync(workFolder, cancellationToken);

        return scopePackage == null ? allFiles
            : allFiles.Where(file =>
            {
                EnsureFileDetails(file);
                return file.Segments.Count > 1 && // Skip loose files at the work folder root.
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
        await EnumerateAsync(workFolder, string.Empty, files, cancellationToken);

        lock (_cacheLock)
        {
            _cachedFiles?.Clear();
            _cachedFiles ??= files;
            return _cachedFiles;
        }
    }

    private static async Task EnumerateAsync(StorageFolder folder, string prefix, List<CachedFile> files, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (StorageFile file in await folder.GetFilesAsync())
        {
            files.Add(new CachedFile(prefix + file.Name, file));
        }

        foreach (StorageFolder subFolder in await folder.GetFoldersAsync())
        {
            await EnumerateAsync(subFolder, prefix + subFolder.Name + PathSeparator, files, cancellationToken);
        }
    }

    public static async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery? query, CancellationToken cancellationToken = default)
    {
        if (query == null || string.IsNullOrWhiteSpace(query.QueryText)) return [];

        string term = query.QueryText.Trim();
        int maxResults = query.MaxResults > 0 ? query.MaxResults : 50;
        List<SearchResult> results = [];

        List<CachedFile> files = await GetTargetFilesAsync(query.ScopePackage, cancellationToken);
        if (files.Count == 0) return results;

        bool searchContent = query.Locations.HasFlag(SearchLocations.Content);
        bool searchMetadata = query.Locations.HasFlag(SearchLocations.Metadata);

        // Match file names/titles first.
        foreach (CachedFile file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureFileDetails(file);
            List<int> indices = [];
            file.Title.SearchTextAndFindAllMatches(ref indices, term, matchCase: query.MatchCase, matchWholeWord: query.MatchWholeWord);
            if (indices.Count == 0) continue;

            SearchSnippet? snippet = CreateWindowSnippet("Title", file.Title, indices[0], term.Length);
            results.Add(CreateResult(file, snippet != null ? [snippet] : []));
            indices.Clear();

            if (results.Count >= maxResults) return results;
        }

        // Search within files: text content or metadata.
        if (searchContent || searchMetadata)
        {
            foreach (CachedFile file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<SearchSnippet>? snippets = await ScanFileAsync(file, term, query, searchContent, searchMetadata, cancellationToken);
                if (snippets?.Count > 0)
                {
                    results.Add(CreateResult(file, snippets));
                    if (results.Count >= maxResults) return results;
                }
            }
        }

        return results;
    }

    private static SearchResult CreateResult(CachedFile file, IReadOnlyList<SearchSnippet> snippets) =>
        new(file.Kind, file.Title, BreadcrumbOf(file), file.RelativePath, snippets);

    private static IReadOnlyList<string> BreadcrumbOf(CachedFile file)
    {
        EnsureFileDetails(file);
        var segments = file.Segments!;
        return segments.Count > 1 ? segments.Take(segments.Count - 1).ToArray() : [];
    }

    private static string? PackageIdOf(CachedFile file)
    {
        EnsureFileDetails(file);
        // The first segment of the path corresponds to the package folder.
        return file.Segments!.Count > 0 ? file.Segments[0] : null;
    }

    private static void EnsureFileDetails(CachedFile file)
    {
        if (file.Segments != null) return;

        (string folder, string fileName, string extension) = GetDetailsFromFilePath(file.RelativePath);

        file.Segments = folder.Split(PathSeparator);
        file.Kind = GetKindFromExtension(extension);
        file.Title = fileName;
    }

    private static async Task<List<SearchSnippet>?> ScanFileAsync(CachedFile file, string term, SearchQuery query, bool searchContent, bool searchMetadata, CancellationToken cancellationToken)
    {
        try
        {
            return file.Kind switch
            {
                SearchResultKind.Markdown or SearchResultKind.Csv when searchContent =>
                    await ScanTextFileAsync(file, term, query, cancellationToken),
                SearchResultKind.Metadata when searchMetadata =>
                    await ScanJsonFileAsync(file, term, query, cancellationToken),
                _ => null // Skip the rest.
            };
        }
        catch (OperationCanceledException ex)
        {
            throw ex;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<List<SearchSnippet>> ScanTextFileAsync(CachedFile file, string term, SearchQuery query, CancellationToken cancellationToken)
    {
        string text = await FileIO.ReadTextAsync(file.File);

        List<int> indices = [];
        text.SearchTextAndFindAllMatches(ref indices, term, matchCase: query.MatchCase, matchWholeWord: query.MatchWholeWord);
        List<SearchSnippet> snippets = [];
        HashSet<string> usedSnippets = []; // Collapse multiple matches within the same line(s).

        foreach (int index in indices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SearchSnippet? snippet = CreateLineSnippet("Content", text, index, term.Length);
            if (snippet == null || !usedSnippets.Add(snippet.Pre + '\n' + snippet.Match + '\n' + snippet.Post))
                continue;

            snippets.Add(snippet);
            if (snippets.Count >= MaxSnippetsPerFile) break;
        }
        usedSnippets.Clear();
        indices.Clear();
        return snippets;
    }

    // NOTE: Even if we search for a text file, be it description or tag.
    // Only the json file containing the relevant metadata will be fetched.
    // This is due to a design flaw since individual files won't have separate metadata.
    // The metadata will probably be stored in a category or package.
    // Only those files will be pointed in here.
    private static async Task<List<SearchSnippet>> ScanJsonFileAsync(CachedFile file, string term, SearchQuery query, CancellationToken cancellationToken)
    {
        string json = await FileIO.ReadTextAsync(file.File);
        cancellationToken.ThrowIfCancellationRequested();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch { return []; }

        using (document)
        {
            List<SearchSnippet> snippets = [];
            string? title = null;
            string? description = null;

            void MatchField(string field, string? value)
            {
                if (string.IsNullOrEmpty(value)) return;

                List<int> indices = [];
                value.SearchTextAndFindAllMatches(ref indices, term, matchCase: query.MatchCase, matchWholeWord: query.MatchWholeWord);
                if (indices.Count > 0 && snippets.Count < MaxSnippetsPerFile &&
                    CreateWindowSnippet(field, value, indices[0], term.Length) is SearchSnippet snippet)
                {
                    snippets.Add(snippet);
                }
                indices.Clear();
            }

            void ScanProperties(JsonProperty prop)
            {
                switch (prop.Name)
                {
                    case "Title":
                        MatchField("Title", prop.Value.GetString());
                        break;
                    case "Description":
                        MatchField("Description", prop.Value.GetString());
                        break;
                    case "Tags" when prop.Value.ValueKind == JsonValueKind.Array:
                        foreach (var tag in prop.Value.EnumerateArray())
                            MatchField("Tags", tag.GetString());
                        break;
                    case "Id":
                        MatchField("Id", prop.Value.GetString());
                        break;
                }
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Package or Category.
                    if ((property.Name == "Contents" || property.Name == "Items")
                        && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        // This will only look upto 1 level of nesting.
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            foreach (var prop in item.EnumerateObject())
                            {
                                ScanProperties(prop);
                            }
                        }
                    }
                    else ScanProperties(property);
                }
            }

            if (!string.IsNullOrWhiteSpace(title)) file.Title = title!;

            return snippets;
        }
    }

    public static async Task<IResource?> LoadResultResourceAsync(SearchResult? result)
    {
        if (result == null) return null;

        var segments = GetSegmentsFromPath(result.RelativePath);
        if (segments.Count() == 0) return null;

        PackageResource? root = FindRootPackage(segments[0]);
        if (root == null) return null;

        if (!root.HasInitialized)
            await ResourceHelper.LoadResourceAsync(root);

        IResource current = root;
        // Skip the package folder;
        foreach (string segment in segments.Skip(1))
        {
            IResource? child = await FindChildAsync(current, segment);
            if (child == null) return null;

            // Loads the child's own file/metadata and initializes it against its parent.
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
        // If not loaded the children will be just metadata path stubs.
        if (!parent.HasInitialized)
        {
            await ResourceHelper.LoadResourceAsync(parent, parent.ParentResource);
        }

        IResource? match = FindChildByTitle(parent, title);

        return match;
    }

    private static IResource? FindChildByTitle(IResource parent, string title)
    {
        foreach (IResource child in parent.ChildrenResources ?? [])
        {
            if (string.Equals(child.Title, title))
                return child;

            // Split metadata stubs have no title till loaded, but their
            // metadata path ends with the expected resource name.
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
        foreach (var seg in folder.Split(PathSeparator))
        {
            if (seg.Length > 0) results.Add(seg);
        }
        results.Add(fileName);
        return results;
    }

    private static SearchSnippet? CreateLineSnippet(string field, string? text, int matchIndex, int termLength, int contextLines = 1)
    {
        if (string.IsNullOrEmpty(text) || matchIndex < 0 || matchIndex >= text.Length || termLength <= 0)
            return null;

        (int lineStart, int lineEnd) = GetLineBounds(text, matchIndex);

        // Extend by the requested number of adjacent lines in both directions.
        for (int i = 0; i < contextLines && lineStart > 0; i++)
            lineStart = GetLineBounds(text, System.Math.Max(0, lineStart - 2)).Start;

        for (int i = 0; i < contextLines && lineEnd < text.Length; i++)
            lineEnd = GetLineBounds(text, System.Math.Min(text.Length - 1, lineEnd + 1)).End;

        // The match may span outside the primary line bounds (multi-line match); clamp it.
        int matchStart = System.Math.Clamp(matchIndex, lineStart, System.Math.Max(lineStart, lineEnd));
        int matchEnd = System.Math.Min(matchIndex + termLength, lineEnd);
        return CreateSnippet(field, text, lineStart, matchStart, matchEnd, lineEnd);
    }

    private static SearchSnippet? CreateWindowSnippet(string field, string? text, int matchIndex, int termLength, int radius = 60)
    {
        if (string.IsNullOrEmpty(text) || matchIndex < 0 || matchIndex >= text.Length || termLength <= 0)
            return null;

        int preStart = System.Math.Max(0, matchIndex - radius);
        int postEnd = System.Math.Min(text.Length, matchIndex + termLength + radius);
        int matchEnd = System.Math.Min(matchIndex + termLength, text.Length);
        return CreateSnippet(field, text, preStart, matchIndex, matchEnd, postEnd);
    }

    private static SearchSnippet? CreateSnippet(string field, string text, int start, int matchStart, int matchEnd, int end)
    {
        ReadOnlySpan<char> span = text.AsSpan();
        string pre = CollapseWhitespace(span[start..matchStart], prefix: start > 0 ? Ellipsis : null);
        string match = CollapseWhitespace(span[matchStart..matchEnd]);
        string post = CollapseWhitespace(span[matchEnd..end], suffix: end < text.Length ? Ellipsis : null);

        if (pre.Length == 0 && match.Length == 0 && post.Length == 0)
            return null;

        return new SearchSnippet(field, pre, match, post);
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

    private static (int Start, int End) GetLineBounds(string text, int index)
    {
        int start = text.LastIndexOf('\n', System.Math.Max(0, System.Math.Min(index, text.Length) - 1));
        start = start < 0 ? 0 : start + 1;

        int end = text.IndexOf('\n', index);
        end = end < 0 ? text.Length : end;
        if (end > start && text[end - 1] == '\r') end--;

        return (start, end);
    }

    private static string CollapseWhitespace(ReadOnlySpan<char> text, string? prefix = null, string? suffix = null)
    {
        var sb = new StringBuilder(text.Length);
        sb.Append(prefix);
        bool lastWasSpace = false;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                lastWasSpace = true;
            }
            else
            {
                if (lastWasSpace && sb.Length > 0) sb.Append(' ');
                lastWasSpace = false;
                sb.Append(ch);
            }
        }
        sb.Append(suffix);
        return sb.ToString();
    }
}
