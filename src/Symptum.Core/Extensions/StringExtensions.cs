using System;

namespace Symptum.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="string1">The larger string being compared.</param>
    /// <param name="string2">The smaller string to seek.</param>
    /// <param name="offset">The offset to start the comparison from.</param>
    /// <param name="endChar">The character which should be present in the bigger string after all the characters of smaller string.</param>
    /// <returns><see langword="true"/> if the all the characters of <paramref name="string2"/> are present in <paramref name="string1"/>;
    /// otherwise, <see langword="false"/>.</returns>
    public static bool Contains(this string? string1, string? string2, int offset, char? endChar = null)
    {
        if (string1 == null || string2 == null
            || offset < 0 || offset >= string2.Length)
            return false;

        for (int i = offset; i < string2.Length; i++)
        {
            if (string2[i] != string1[i])
                return false;
        }

        if (string2.Length == string1.Length || endChar == null
            || (string1.Length > string2.Length && string1[string2.Length] == endChar))
            return true;

        return false;
    }

    public static bool Contains(this string? text, string? value, bool matchCase = false, bool matchWholeWord = false)
    {
        if (text == null || value == null || value.Length > text.Length)
            return false;

        StringComparison comparisonType = matchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

        ReadOnlySpan<char> span = text.AsSpan();
        ReadOnlySpan<char> searchSpan = value.AsSpan();

        int searchLength = searchSpan.Length;
        int textLength = span.Length;

        for (int _start = 0; _start <= textLength - searchLength; _start++)
        {
            if (span.Slice(_start, searchLength).Equals(searchSpan, comparisonType)
                && (!matchWholeWord || IsWholeWordMatch(span, _start, searchLength)))
                return true;
        }

        return false;
    }

    public static int[] SearchTextAndFindAllMatches(this string? text, string? searchText, int searchStart = 0, int searchEnd = 0, bool matchCase = false, bool matchWholeWord = false)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchText) || searchText.Length > text.Length || searchStart < 0)
            return [];

        StringComparison comparisonType = matchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

        ReadOnlySpan<char> span = text.AsSpan();
        ReadOnlySpan<char> searchSpan = searchText.AsSpan();

        List<int> results = [];

        int searchLength = searchSpan.Length;
        int textLength = searchEnd > 0 ? searchEnd : span.Length;

        for (int _start = searchStart; _start <= textLength - searchLength; _start++)
        {
            if (span.Slice(_start, searchLength).Equals(searchSpan, comparisonType)
                && (!matchWholeWord || IsWholeWordMatch(span, _start, searchLength)))
                results.Add(_start);
        }

        return [.. results];
    }

    private static bool IsWholeWordMatch(ReadOnlySpan<char> span, int start, int length)
    {
        int end = start + length;
        return (start == 0 || !char.IsLetterOrDigit(span[start - 1])) &&
               (end == span.Length || !char.IsLetterOrDigit(span[end]));
    }

    public static (int lineIndex, int columnIndex) GetLineAndColumnIndex(this string? text, int position)
    {
        if (string.IsNullOrEmpty(text) || position < 0 || position > text.Length)
            return (0, 0);

        int line = 1;
        int lastLineStart = 0;

        for (int i = 1; i <= position; i++)
        {
            if (i > 1 && text[i - 1] == '\r' || text[i - 1] == '\n')
            {
                line++;
                lastLineStart = i;

                // Skip the \n in CRLF
                if (text[i - 1] == '\r' && i < text.Length && text[i] == '\n')
                    i++;
            }
        }

        return (line, position - lastLineStart + 1);
    }
}
