namespace Symptum.Markdown.Mermaid;

public static class MermaidSyntax
{
    private static readonly HashSet<string> MermaidLanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "diagram-mermaid",
        "mmd",
        "mermaid",
        "mermaidjs"
    };

    public static bool TryParseDescriptor(
        string? info,
        string? arguments,
        out string normalizedInfo,
        out string normalizedArguments)
    {
        normalizedInfo = "mermaid";
        normalizedArguments = string.Empty;

        var normalizedDescriptor = NormalizeDescriptor(info);
        if (IsMermaidLanguage(normalizedDescriptor))
        {
            normalizedArguments = arguments?.Trim() ?? string.Empty;
            return true;
        }

        if (!string.Equals(normalizedDescriptor, "diagram", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var trimmedArguments = arguments?.Trim() ?? string.Empty;
        if (trimmedArguments.Length == 0)
        {
            return false;
        }

        var separatorIndex = trimmedArguments.IndexOfAny([' ', '\t']);
        var firstToken = separatorIndex >= 0 ? trimmedArguments[..separatorIndex] : trimmedArguments;
        if (!IsMermaidLanguage(firstToken))
        {
            return false;
        }

        normalizedArguments = separatorIndex >= 0
            ? trimmedArguments[(separatorIndex + 1)..].TrimStart()
            : string.Empty;
        return true;
    }

    public static string NormalizeCode(string source)
    {
        return source
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .TrimEnd('\n');
    }

    private static bool IsMermaidLanguage(string? languageHint)
    {
        return MermaidLanguageAliases.Contains(NormalizeDescriptor(languageHint));
    }

    private static string NormalizeDescriptor(string? languageHint)
    {
        if (string.IsNullOrWhiteSpace(languageHint))
        {
            return string.Empty;
        }

        var trimmed = languageHint.Trim();
        var separatorIndex = trimmed.IndexOfAny([' ', '\t', ',', ';', '{', '(']);
        var normalized = separatorIndex >= 0 ? trimmed[..separatorIndex] : trimmed;
        return normalized.Trim().Trim('.').ToLowerInvariant();
    }
}
