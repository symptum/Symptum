namespace Symptum.Markdown.Mermaid;

public static class MermaidStandardPreprocessor
{
    public static IReadOnlyList<string> Preprocess(string normalizedSource)
    {
        var rawLines = normalizedSource.Split('\n', StringSplitOptions.None);
        var result = new List<string>(rawLines.Length);
        var index = 0;

        while (index < rawLines.Length && string.IsNullOrWhiteSpace(rawLines[index]))
        {
            index++;
        }

        if (index < rawLines.Length && string.Equals(rawLines[index].Trim(), "---", StringComparison.Ordinal))
        {
            index++;
            while (index < rawLines.Length && !string.Equals(rawLines[index].Trim(), "---", StringComparison.Ordinal))
            {
                index++;
            }

            if (index < rawLines.Length)
            {
                index++;
            }
        }

        var inDirective = false;
        for (; index < rawLines.Length; index++)
        {
            var rawLine = rawLines[index];
            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (inDirective)
            {
                if (trimmed.Contains("}%%", StringComparison.Ordinal))
                {
                    inDirective = false;
                }

                continue;
            }

            if (trimmed.StartsWith("%%{", StringComparison.Ordinal))
            {
                if (!trimmed.Contains("}%%", StringComparison.Ordinal))
                {
                    inDirective = true;
                }

                continue;
            }

            var withoutComment = StripInlineComment(rawLine);
            if (string.IsNullOrWhiteSpace(withoutComment))
            {
                continue;
            }

            result.Add(withoutComment.TrimEnd());
        }

        return result;
    }

    private static string StripInlineComment(string line)
    {
        var commentIndex = line.IndexOf("%%", StringComparison.Ordinal);
        return commentIndex >= 0 ? line[..commentIndex].TrimEnd() : line;
    }
}
