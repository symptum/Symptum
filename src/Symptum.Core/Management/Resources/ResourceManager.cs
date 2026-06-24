using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Symptum.Core.Helpers.FileHelper;
using Symptum.Core.Extensions;
using Symptum.Core.Subjects;

namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    public static readonly string DefaultUriScheme = "symptum://";

    public static readonly Uri DefaultUri = new(DefaultUriScheme);

    private static readonly ObservableCollection<IResource> _resources = [];

    public static ObservableCollection<IResource> Resources { get => _resources; }

    public static Uri GetAbsoluteUri(string path) => new(DefaultUriScheme + path);

    #region Resource File Handling

    /// <summary>
    /// Gets the file name of the resource.
    /// </summary>
    /// <param name="resource">The resource to get the file name from.</param>
    /// <returns>The file name of the resource.</returns>
    public static string? GetResourceFileName(IResource? resource) => resource?.Title;

    /// <summary>
    /// Gets the absolute folder path of the resource including its path.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <returns>The absolute folder path of the resource including its path.</returns>
    public static string GetAbsoluteFolderPath(IResource? resource) =>
        BuildResourcePath(resource, includeSelf: true, stopAtPackage: false);

    /// <summary>
    /// Gets the absolute folder path of the resource.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <returns>The absolute folder path of the resource.</returns>
    public static string GetAbsoluteResourceFolderPath(IResource? resource) =>
        BuildResourcePath(resource, includeSelf: false, stopAtPackage: false);

    /// <summary>
    /// Gets the folder path of the resource relative to its parent <see cref="PackageResource"/>.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <returns>The folder path of the resource relative to its parent <see cref="PackageResource"/></returns>
    public static string GetRelativeResourceFolderPath(IResource? resource)
    {
        if (resource is PackageResource)
        {
            return PathSeparator.ToString();
        }

        return BuildResourcePath(resource, includeSelf: false, stopAtPackage: true);
    }

    /// <summary>
    /// Gets the file path of the resource relative to its parent <see cref="PackageResource"/>.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <param name="extension">The file extension of the resource.</param>
    /// <returns>The file path of the resource relative to its parent <see cref="PackageResource"/></returns>
    public static string GetResourceFilePath(IResource? resource, string? extension)
    {
        string path = GetRelativeResourceFolderPath(resource);
        return path + GetResourceFileName(resource) + extension;
    }

    private static string BuildResourcePath(IResource? resource, bool includeSelf, bool stopAtPackage)
    {
        var segments = new List<string?>();
        IResource? current = includeSelf ? resource : resource?.ParentResource;

        while (current != null)
        {
            if (stopAtPackage && current is PackageResource)
            {
                segments.Add(GetResourceFileName(current));
                break;
            }

            segments.Add(GetResourceFileName(current));
            current = current.ParentResource;
        }

        var path = new StringBuilder(PathSeparator.ToString());
        for (int index = segments.Count - 1; index >= 0; index--)
        {
            path.Append(segments[index]);
            path.Append(PathSeparator);
        }

        return path.ToString();
    }

    public static void LoadResourceFileText(TextFileResource? fileResource, string text) => fileResource?.ReadFileText(text);

    public static string? WriteResourceFileText(TextFileResource? fileResource) => fileResource?.WriteFileText();

    public static PackageResource? LoadPackageFromMetadata(string metadata) => JsonSerializer.Deserialize<PackageResource>(metadata);

    public static void LoadResourceMetadata(MetadataResource? resource, string metadata) => resource?.LoadMetadata(metadata);

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
    };

    public static string? WritePackageMetadata(PackageResource? package) => JsonSerializer.Serialize(package, options);

    public static string? WriteResourceMetadata<T>(T? resource) where T : MetadataResource => JsonSerializer.Serialize(resource, resource?.GetType(), options);

    #endregion

    #region Resource Fetching

    #region Get Parent

    public static bool TryGetParentOfType<T>(IResource? resource, [NotNullWhen(true)] out T? parent,
        Func<T?, bool>? condition = null) where T : IResource
    {
        parent = default;
        if (resource == null) return false;

        for (IResource? current = resource; current != null; current = current.ParentResource)
        {
            if (current.ParentResource is T p)
            {
                if (condition == null || condition(p))
                {
                    parent = p;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool TryGetSavableParent(IResource? resource, [NotNullWhen(true)] out IMetadataResource? parent) =>
        TryGetParentOfType(resource, out parent, x => x is PackageResource || (x != null && x.SplitMetadata));

    public static bool TryGetParentPackage(IResource? resource, [NotNullWhen(true)] out PackageResource? package) =>
        TryGetParentOfType(resource, out package);

    #endregion

    #region By Id

    /// <summary>
    /// Tries to get a resource with the given id from the global resource list.
    /// </summary>
    /// <param name="id">The id of the resource.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the resource was found, false otherwise.</returns>
    public static bool TryGetResourceById(string? id, [NotNullWhen(true)] out IResource? resource) =>
        TryGetResourceById(id, _resources, out resource);

    /// <summary>
    /// Tries to get a resource with the given id within the specified resources.
    /// </summary>
    /// <param name="id">The id of the resource.</param>
    /// <param name="resources">The list of resources to search in.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the resource was found, false otherwise.</returns>
    public static bool TryGetResourceById(string? id, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource)
    {
        if (TryGetAvailableChildResourceById(id, resources, out IResource? _resource))
        {
            resource = _resource;
            return true;
        }

        resource = null;
        return false;
    }

    /// <summary>
    /// Tries to get a resource with the given id from the global resource list.
    /// If an exact match is not found, it will return the nearest matching ancestor.
    /// </summary>
    /// <param name="id">The id of the resource.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the exact resource was found, false otherwise.</returns>
    public static bool TryGetAvailableChildResourceById(string? id, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceById(id, _resources, out resource);

    /// <summary>
    /// Tries to get a resource with the given id within the specified resources.
    /// If an exact match is not found, it will return the nearest matching ancestor.
    /// </summary>
    /// <param name="id">The id of the resource.</param>
    /// <param name="resources">The list of resources to search in.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the exact resource was found, false otherwise.</returns>
    public static bool TryGetAvailableChildResourceById(string? id, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceByProperty(id, IdEquals, IdContains, resources, out resource);

    private static bool IdEquals(string? id, IResource resource) => id?.Equals(resource.Id) ?? false;

    // Resource's id would most probably be equal or shorter in length than the required id
    // So we take the resource's id and compare all the characters in it with the other
    // If it matches, we return true
    // Else, the current resource tree doesn't contain the required id
    private static bool IdContains(string? requiredId, IResource resource, int offset = 0) =>
        requiredId?.Contains(resource.Id, offset, '.') ?? false;

    #endregion

    #region By Uri

    /// <summary>
    /// Tries to get a resource with the given Uri from the global resource list.
    /// </summary>
    /// <param name="uri">The Uri of the resource.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the resource was found, false otherwise.</returns>
    public static bool TryGetResourceByUri(Uri? uri, [NotNullWhen(true)] out IResource? resource) =>
        TryGetResourceByUri(uri, _resources, out resource);

    /// <summary>
    /// Tries to get a resource with the given Uri within the specified resources.
    /// </summary>
    /// <param name="uri">The Uri of the resource.</param>
    /// <param name="resources">The list of resources to search in.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the resource was found, false otherwise.</returns>
    public static bool TryGetResourceByUri(Uri? uri, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource)
    {
        if (TryGetAvailableChildResourceByUri(uri, resources, out IResource? _resource))
        {
            resource = _resource;
            return true;
        }

        resource = null;
        return false;
    }

    /// <summary>
    /// Tries to get a resource with the given Uri from the global resource list.
    /// If an exact match is not found, it will return the nearest matching ancestor.
    /// </summary>
    /// <param name="uri">The Uri of the resource.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the exact resource was found, false otherwise.</returns>
    public static bool TryGetAvailableChildResourceByUri(Uri? uri, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceByUri(uri, _resources, out resource);

    /// <summary>
    /// Tries to get a resource with the given Uri within the specified resources.
    /// If an exact match is not found, it will return the nearest matching ancestor.
    /// </summary>
    /// <param name="uri">The Uri of the resource.</param>
    /// <param name="resources">The list of resources to search in.</param>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the exact resource was found, false otherwise.</returns>
    public static bool TryGetAvailableChildResourceByUri(Uri? uri, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceByProperty(uri, UriEquals, UriContains, resources, out resource);

    private static bool UriEquals(Uri? uri, IResource resource) => uri?.Equals(resource.Uri) ?? false;

    // Resource's uri would most probably be equal or shorter in length than the required uri
    // So we take the resource's uri and compare all the characters in it with the other
    // If it matches, we return true
    // Else, the current resource tree doesn't contain the required uri
    private static bool UriContains(Uri? requiredUri, IResource resource, int offset = 0) =>
        requiredUri?.ToString().Contains(resource.Uri?.ToString(), offset, '/') ?? false;

    #endregion

    private static bool TryGetAvailableChildResourceByProperty<T>(T? value, Func<T?, IResource, bool> comparer, Func<T?, IResource, int, bool> contains, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource)
    {
        resource = null;
        if (value == null || resources == null)
        {
            return false;
        }

        // Use a stack to keep track of the current resources and their offsets.
        Stack<(IReadOnlyList<IResource>? Resources, int Offset)> frames = new();
        frames.Push((resources, 0));

        while (frames.Count > 0)
        {
            // Pop the current resources and offset from the stack
            // Thus the stack will be empty.
            var (currentResources, currentOffset) = frames.Pop();
            if (currentResources == null)
            {
                continue;
            }

            IResource? matchedParent = null;
            foreach (IResource currentResource in currentResources)
            {
                if (comparer(value, currentResource))
                {
                    frames.Clear();
                    resource = currentResource;
                    matchedParent = null;
                    return true;
                }

                if (contains(value, currentResource, currentOffset))
                {
                    matchedParent = currentResource;
                    if (currentResource.ChildrenResources is { Count: > 0 })
                    {
                        frames.Push((currentResource.ChildrenResources, currentResource.Uri?.ToString().Length ?? 0));
                    }
                    break;
                }
            }

            if (matchedParent != null)
            {
                resource = matchedParent;
            }
        }

        return false;
    }

    #endregion

    #region Registering and Unregistering Resources

    public static void RegisterResource(IResource? resource)
    {
        if (resource is Subject subject)
        {
            SubjectsManager.RegisterSubject(subject);
        }
    }

    public static void UnregisterResource(IResource? resource)
    {
        if (resource is Subject subject)
        {
            SubjectsManager.UnregisterSubject(subject);
        }
    }

    #endregion
}
