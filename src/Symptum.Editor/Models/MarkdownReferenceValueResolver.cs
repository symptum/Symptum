using Symptum.Core.Management.Resources;
using Symptum.Markdown.Reference;
using Symptum.UI.Markdown;

namespace Symptum.Editor.Models;

public sealed class MarkdownReferenceValueResolver : IReferenceValueResolver
{
    private readonly MarkdownFileResource? _resource;

    public MarkdownReferenceValueResolver(MarkdownFileResource? resource)
    {
        _resource = resource;
    }

    // It runs synchronously for now since all the resources are loaded while loading in Editor.
    // But if the project grows huge, we must find ways to unload and free up unnecessary resources.
    // In such cases, we might need to load the dependencies asynchronously and resolve them.
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
