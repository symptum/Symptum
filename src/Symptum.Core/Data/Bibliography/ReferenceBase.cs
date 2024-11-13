using System.Diagnostics.CodeAnalysis;

namespace Symptum.Core.Data.Bibliography;

public abstract record ReferenceBase
{
    #region Properties

    public string? Id { get; init; }

    #endregion

    public static bool TryParse(string? text, [NotNullWhen(true)] out ReferenceBase? reference)
    {
        bool parsed = false;
        reference = null;

        if (!string.IsNullOrEmpty(text))
        {
            if (text.StartsWith("@book?"))
            {
                if (BookReference.TryParse(text, out BookReference? bookReference))
                {
                    parsed = true;
                    reference = bookReference;
                }
            }
        }

        return parsed;
    }

    public virtual string GetPreviewText() => string.Empty;
}
