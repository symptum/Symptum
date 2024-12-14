using System.Diagnostics.CodeAnalysis;

namespace Symptum.Core.Data.Bibliography;

public abstract record ReferenceBase
{
    public static bool TryParse(string? text, [NotNullWhen(true)] out ReferenceBase? reference)
    {
        bool parsed = false;
        reference = null;

        if (!string.IsNullOrEmpty(text))
        {
            if (text.StartsWith("@book?"))
            {
                if (PresetBookReference.TryParse(text, out PresetBookReference? bookReference))
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
