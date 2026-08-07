using System.Diagnostics.CodeAnalysis;

namespace Symptum.Markdown.Reference;

public static class ReferenceInlineHelper
{
    /// <summary>
    /// Parses the content of a <see cref="ReferenceInline"/> which is in the form of
    /// <c>paramId#entryIndex.quantityIndex</c> (an optional leading '@' is also accepted)
    /// where both indices are optional.
    /// </summary>
    public static bool TryParse(string? content, [NotNullWhen(true)] out string? parameterId, out int entryIndex, out int quantityIndex)
    {
        parameterId = null;
        entryIndex = 0;
        quantityIndex = 0;
        if (string.IsNullOrWhiteSpace(content)) return false;

        ReadOnlySpan<char> span = content.Trim();
        if (span[0] == '@')
            span = span[1..];

        if (span.Length == 0) return false;

        int hashIndex = span.IndexOf('#');
        int dotIndex = span.IndexOf('.');

        if (hashIndex >= 0)
        {
            if (hashIndex == 0) return false;

            parameterId = span[..hashIndex].ToString();
            ReadOnlySpan<char> rest = span[(hashIndex + 1)..];

            if (rest.Length == 0 || !char.IsAsciiDigit(rest[0])) return false;

            int i = 0;
            while (i < rest.Length && char.IsAsciiDigit(rest[i]))
                i++;

            if (!int.TryParse(rest[..i], out entryIndex)) return false;

            rest = rest[i..];

            if (rest.Length > 0 && rest[0] == '.')
            {
                int j = 1;
                while (j < rest.Length && char.IsAsciiDigit(rest[j]))
                    j++;

                if (j == 1 || !int.TryParse(rest[1..j], out quantityIndex)) return false;

                rest = rest[j..];
            }

            return rest.Length == 0;
        }

        if (dotIndex >= 0)
        {
            if (dotIndex == 0) return false;

            parameterId = span[..dotIndex].ToString();
            ReadOnlySpan<char> rest = span[(dotIndex + 1)..];

            if (rest.Length == 0 || !char.IsAsciiDigit(rest[0])) return false;

            int i = 0;
            while (i < rest.Length && char.IsAsciiDigit(rest[i]))
                i++;

            if (!int.TryParse(rest[..i], out quantityIndex)) return false;

            return rest[i..].Length == 0;
        }

        parameterId = span.ToString();
        return true;
    }

    /// <summary>
    /// Returns true if the character can be part of a parameter id (i.e. a letter, digit, '_' or '-').
    /// </summary>
    public static bool IsParameterIdChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

    /// <summary>
    /// Builds the reference inline syntax (i.e. <c>@paramId#entryIndex.quantityIndex</c>) from the given components.
    /// </summary>
    public static string BuildSyntax(string? parameterId, int entryIndex = 0, int quantityIndex = 0) =>
        $"@{parameterId}#{entryIndex}.{quantityIndex}";
}
