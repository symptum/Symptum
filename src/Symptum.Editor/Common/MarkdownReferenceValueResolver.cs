using Symptum.Core.Management.Resources;
using Symptum.Markdown;
using Symptum.Markdown.Reference;
using Symptum.UI.Markdown;

namespace Symptum.Editor.Common;

/// <summary>
/// Resolves <see cref="ReferenceInline"/> values against the dependencies of a <see cref="MarkdownFileResource"/>.
/// </summary>
public sealed class MarkdownReferenceValueResolver : IReferenceValueResolver
{
    private readonly MarkdownFileResource? _resource;

    public MarkdownReferenceValueResolver(MarkdownFileResource? resource)
    {
        _resource = resource;
    }

    public Task<(string Text, string Url)?> ResolveAsync(string referenceSyntax)
    {
        (string, string)? result = null;

        if (ReferenceInlineHelper.TryParse(referenceSyntax, out string? parameterId, out int entryIndex, out int quantityIndex)
            && ReferenceValueResolver.TryResolveValue(_resource, parameterId, entryIndex, quantityIndex, out string? text, out string? url))
        {
            result = (text, url);
        }

        return Task.FromResult(result);
    }
}
