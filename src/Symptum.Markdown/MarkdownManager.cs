using System.Text;
using Markdig;
using Symptum.Core.Management.Resources;
using Symptum.Markdown.Embedding;
using Symptum.Markdown.Reference;

namespace Symptum.Markdown;

public static class MarkdownManager
{
    public static readonly MarkdownPipeline Pipeline;

    static MarkdownManager()
    {
        Pipeline = new MarkdownPipelineBuilder()
            .UseAlertBlocks()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseListExtras()
            .UseTaskLists()
            .UsePipeTables()
            .UseGridTables()
            .UseAutoIdentifiers(Markdig.Extensions.AutoIdentifiers.AutoIdentifierOptions.GitHub)
            .Use<ReferenceInlineExtension>()
            .Use<ExportBlockExtension>()
            .Use<ImportBlockExtension>()
            .Build();
    }

    // Removes ExportBlock syntax and writes the content directly.
    // Replaces ImportBlock syntax with content of the referenced ExportBlock.
    // NOTE: It doesn't support nested ExportBlocks for now.
    public static string? GetOptimizedMarkdown(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;

        ReadOnlySpan<char> span = markdown.AsSpan();
        var localExports = new Dictionary<string, string>(StringComparer.Ordinal);

        // Pass 1: collect all export block IDs and their content
        int pos = 0;
        while (pos < span.Length)
        {
            ReadOnlySpan<char> line = ReadLine(span, ref pos);
            ReadOnlySpan<char> trimmed = line.TrimStart();

            if (IsExportBoundary(trimmed, out ReadOnlySpan<char> id) && id.Length > 0)
            {
                localExports[id.ToString()] = ReadExportContent(span, ref pos);
            }
        }

        // Pass 2: build optimized markdown
        pos = 0;
        var result = new StringBuilder(span.Length);
        bool needNewline = false;

        while (pos < span.Length)
        {
            ReadOnlySpan<char> line = ReadLine(span, ref pos);
            ReadOnlySpan<char> trimmed = line.TrimStart();

            if (IsExportBoundary(trimmed, out _))
            {
                // Export block: output content lines directly (skip wrapper)
                while (pos < span.Length)
                {
                    ReadOnlySpan<char> contentLine = ReadLine(span, ref pos);
                    if (IsExportBoundary(contentLine.TrimStart(), out _))
                        break;

                    AppendWithNewline(result, contentLine, ref needNewline);
                }
            }
            else if (IsImportLine(trimmed, out ReadOnlySpan<char> importId) && importId.Length > 0)
            {
                string? resolved = ResolveImport(importId.ToString(), localExports);
                if (resolved != null)
                {
                    AppendWithNewline(result, resolved.AsSpan(), ref needNewline);
                }
                else
                {
                    AppendWithNewline(result, line, ref needNewline);
                }
            }
            else
            {
                AppendWithNewline(result, line, ref needNewline);
            }
        }

        return result.ToString();
    }

    private static void AppendWithNewline(StringBuilder sb, ReadOnlySpan<char> content, ref bool needNewline)
    {
        if (needNewline)
            sb.Append('\n');

        sb.Append(content);
        needNewline = true;
    }

    private static ReadOnlySpan<char> ReadLine(ReadOnlySpan<char> text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length)
        {
            char c = text[pos];
            if (c == '\r' || c == '\n')
            {
                ReadOnlySpan<char> line = text[start..pos];
                pos++;
                if (c == '\r' && pos < text.Length && text[pos] == '\n')
                    pos++;

                return line;
            }
            pos++;
        }

        return text[start..];
    }

    private static bool IsExportBoundary(ReadOnlySpan<char> trimmed, out ReadOnlySpan<char> id)
    {
        if (trimmed.Length >= 2 && trimmed[0] == '<' && trimmed[1] == '=')
        {
            id = trimmed[2..].TrimStart();
            return true;
        }

        id = default;
        return false;
    }

    private static bool IsImportLine(ReadOnlySpan<char> trimmed, out ReadOnlySpan<char> id)
    {
        if (trimmed.Length >= 2 && trimmed[0] == '=' && trimmed[1] == '>')
        {
            id = trimmed[2..].TrimStart();
            return true;
        }

        id = default;
        return false;
    }

    private static string ReadExportContent(ReadOnlySpan<char> text, ref int pos)
    {
        var sb = new StringBuilder();
        bool first = true;

        while (pos < text.Length)
        {
            ReadOnlySpan<char> line = ReadLine(text, ref pos);
            if (IsExportBoundary(line.TrimStart(), out _))
                break;

            if (!first)
                sb.Append('\n');

            sb.Append(line);
            first = false;
        }

        return sb.ToString();
    }

    private static string? ResolveImport(string id, Dictionary<string, string> localExports)
    {
        int questionIndex = id.IndexOf('?');
        if (questionIndex >= 0)
        {
            string resourceId = id[..questionIndex];
            string blockId = id[(questionIndex + 1)..];
            return GetExternalExportContent(resourceId, blockId);
        }

        return localExports.TryGetValue(id, out var content) ? content : null;
    }

    private static string? GetExternalExportContent(string resourceId, string blockId)
    {
        if (ResourceManager.TryGetResourceById(resourceId, out var resource)
            && resource is MarkdownFileResource mdResource
            && mdResource.Markdown is string md)
        {
            ReadOnlySpan<char> span = md.AsSpan();
            int pos = 0;

            while (pos < span.Length)
            {
                ReadOnlySpan<char> line = ReadLine(span, ref pos);
                ReadOnlySpan<char> trimmed = line.TrimStart();

                if (IsExportBoundary(trimmed, out ReadOnlySpan<char> exportId)
                    && exportId.Length > 0
                    && exportId.SequenceEqual(blockId.AsSpan()))
                {
                    return ReadExportContent(span, ref pos);
                }
            }
        }

        return null;
    }
}
