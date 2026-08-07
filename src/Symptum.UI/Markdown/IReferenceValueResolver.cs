namespace Symptum.UI.Markdown;

/// <summary>
/// Resolves the values referenced by <see cref="Symptum.Markdown.Reference.ReferenceInline"/> syntax
/// (i.e. <c>@paramId#entryIndex.quantityIndex</c>) into a display text and a url.
/// </summary>
public interface IReferenceValueResolver
{
    /// <summary>
    /// Resolves the given reference syntax into a display text and a url, or returns null if it cannot be resolved.
    /// </summary>
    Task<(string Text, string Url)?> ResolveAsync(string referenceSyntax);
}
