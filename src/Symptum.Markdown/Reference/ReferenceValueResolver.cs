using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;

namespace Symptum.Markdown.Reference;

/// <summary>
/// Resolves <see cref="ReferenceInline"/> values against the dependencies of a <see cref="MarkdownFileResource"/>.
/// </summary>
public static class ReferenceValueResolver
{
    /// <summary>
    /// Tries to resolve the value of a reference inline from the resource's dependencies.
    /// </summary>
    /// <returns>
    /// The display text and the url of the referenced value.
    /// The url is the url of the <see cref="ReferenceValueGroup"/> followed by <c>?{parameterId}#{entryIndex}.{quantityIndex}</c>.
    /// </returns>
    public static bool TryResolveValue(MarkdownFileResource? resource, string? parameterId, int entryIndex, int quantityIndex,
        [NotNullWhen(true)] out string? text, [NotNullWhen(true)] out string? url)
    {
        text = null;
        url = null;
        if (resource == null || resource.Dependencies == null || string.IsNullOrEmpty(parameterId)) return false;

        foreach (var dependency in resource.Dependencies)
        {
            if (dependency is not ReferenceValueGroup group || group.Parameters == null) continue;

            ReferenceValueParameter? parameter = group.Parameters.FirstOrDefault(p => p.Id == parameterId);
            if (parameter == null) continue;

            ReferenceValueEntry? entry = parameter.Entries?.ElementAtOrDefault(entryIndex);
            Quantity? quantity = entry?.Quantities?.ElementAtOrDefault(quantityIndex);

            text = quantity?.ToReadableString()
                ?? entry?.Title
                ?? parameter.Title
                ?? parameterId;

            url = BuildReferenceUrl(group, parameterId, entryIndex, quantityIndex);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Builds the url of a reference value which is the url of the group followed by
    /// <c>?{parameterId}#{entryIndex}.{quantityIndex}</c>.
    /// </summary>
    public static string BuildReferenceUrl(IResource? group, string? parameterId, int entryIndex, int quantityIndex)
    {
        string groupUri = group?.Uri?.ToString() ?? group?.Id ?? string.Empty;
        return $"{groupUri}?{parameterId}#{entryIndex}.{quantityIndex}";
    }
}
