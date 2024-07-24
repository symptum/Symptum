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
        if (string1 != null && string2 != null)
        {
            var span1 = string1.AsSpan();
            var span2 = string2.AsSpan();

            for (int i = offset; i < span2.Length; i++)
            {
                if (span2[i] != span1[i])
                    break;

                if (i == span2.Length - 1)
                {
                    if (span2.Length == span1.Length ||
                        endChar == null ||
                        (span1.Length > i && span1[i + 1] == endChar))
                        return true;
                }
            }
        }

        return false;
    }
}
