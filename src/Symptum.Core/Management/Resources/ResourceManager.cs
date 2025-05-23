using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Symptum.Core.Helpers.FileHelper;
using Symptum.Core.Extensions;
using System.Diagnostics.CodeAnalysis;
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
    public static string GetAbsoluteFolderPath(IResource? resource)
    {
        string _path = PathSeparator.ToString();
        if (resource != null)
        {
            _path = GetAbsoluteFolderPath(resource.ParentResource) + GetResourceFileName(resource) + PathSeparator;
        }
        return _path;
    }

    /// <summary>
    /// Gets the absolute folder path of the resource.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <returns>The absolute folder path of the resource.</returns>
    public static string GetAbsoluteResourceFolderPath(IResource? resource)
    {
        string _path = PathSeparator.ToString();
        if (resource?.ParentResource is IResource parent)
        {
            _path = GetAbsoluteResourceFolderPath(parent) + GetResourceFileName(parent) + PathSeparator;
        }
        return _path;
    }

    /// <summary>
    /// Gets the folder path of the resource relative to its parent <see cref="PackageResource"/>.
    /// </summary>
    /// <param name="resource">The resource to get the path from.</param>
    /// <returns>The folder path of the resource relative to its parent <see cref="PackageResource"/></returns>
    public static string GetRelativeResourceFolderPath(IResource? resource)
    {
        string _path = PathSeparator.ToString();
        if (resource is not PackageResource && resource?.ParentResource is IResource parent)
        {
            _path = GetRelativeResourceFolderPath(parent) + GetResourceFileName(parent) + PathSeparator;
        }
        return _path;
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

        if (resource.ParentResource is T p)
        {
            if (condition == null || condition(p))
            {
                parent = p;
                return true;
            }
        }

        return TryGetParentOfType(resource.ParentResource, out parent, condition);
    }

    public static bool TryGetSavableParent(IResource? resource, [NotNullWhen(true)] out IMetadataResource? parent) =>
        TryGetParentOfType(resource, out parent, x => x is PackageResource || (x != null && x.SplitMetadata));

    public static bool TryGetParentPackage(IResource? resource, [NotNullWhen(true)] out PackageResource? package) =>
        TryGetParentOfType(resource, out package);

    #endregion

    #region From Id

    public static bool TryGetResourceFromId(string? id, [NotNullWhen(true)] out IResource? resource) =>
        TryGetResourceFromId(id, _resources, out resource);

    public static bool TryGetResourceFromId(string? id, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource)
    {
        if (TryGetAvailableChildResourceFromId(id, resources, out IResource? _resource))
        {
            resource = _resource;
            return true;
        }

        resource = null;
        return false;
    }

    public static bool TryGetAvailableChildResourceFromId(string? id, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceFromId(id, _resources, out resource);

    public static bool TryGetAvailableChildResourceFromId(string? id, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource, int offset = 0)
    {
        if (!string.IsNullOrEmpty(id) && resources != null)
        {
            foreach (IResource _resource in resources)
            {
                if (string.Equals(id, _resource.Id))
                {
                    resource = _resource;
                    return true;
                }
                else if (IdContains(id, _resource.Id, offset))
                {
                    bool result = TryGetAvailableChildResourceFromId(id, _resource.ChildrenResources, out resource, _resource.Id?.Length ?? 0);
                    resource ??= _resource; // The parent resource which is most likely to have the child.
                    // Even if we can't find the exact resource we can return this probable parent resource.
                    return result;
                }
            }
        }

        resource = null;
        return false;
    }

    // Resource's id would most probably be equal or shorter in length than the required id
    // So we take the resource's id and compare all the characters in it with the other
    // If it matches, we return true
    // Else, the current resource tree doesn't contain the required id
    private static bool IdContains(string? requiredId, string? resourceId, int offset = 0) =>
        requiredId?.Contains(resourceId, offset, '.') ?? false;

    #endregion

    #region From Uri

    public static bool TryGetResourceFromUri(Uri? uri, [NotNullWhen(true)] out IResource? resource) =>
        TryGetResourceFromUri(uri, _resources, out resource);

    public static bool TryGetResourceFromUri(Uri? uri, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource)
    {
        if (TryGetAvailableChildResourceFromUri(uri, resources, out IResource? _resource))
        {
            resource = _resource;
            return true;
        }

        resource = null;
        return false;
    }

    public static bool TryGetAvailableChildResourceFromUri(Uri? uri, [NotNullWhen(true)] out IResource? resource) =>
        TryGetAvailableChildResourceFromUri(uri, _resources, out resource);

    public static bool TryGetAvailableChildResourceFromUri(Uri? uri, IReadOnlyList<IResource>? resources, [NotNullWhen(true)] out IResource? resource, int offset = 0)
    {
        if (uri != null && resources != null)
        {
            foreach (IResource _resource in resources)
            {
                if (uri == _resource.Uri)
                {
                    resource = _resource;
                    return true;
                }
                else if (UriContains(uri, _resource.Uri, offset))
                {
                    bool result = TryGetAvailableChildResourceFromUri(uri, _resource.ChildrenResources, out resource, _resource.Uri?.ToString().Length ?? 0);
                    resource ??= _resource; // The parent resource which is most likely to have the child.
                    // Even if we can't find the exact resource we can return this probable parent resource.
                    return result;
                }
            }
        }

        resource = null;
        return false;
    }

    // Resource's uri would most probably be equal or shorter in length than the required uri
    // So we take the resource's uri and compare all the characters in it with the other
    // If it matches, we return true
    // Else, the current resource tree doesn't contain the required uri
    private static bool UriContains(Uri? requiredUri, Uri? resourceUri, int offset = 0) =>
        requiredUri?.ToString().Contains(resourceUri?.ToString(), offset, '/') ?? false;

    #endregion

    //public static async Task<AsyncResult<IResource>> TryGetResourceFromIdAsync(string? id)
    //{
    //    return await Task.Run(() =>
    //    {
    //        bool success = TryGetResourceFromId(id, out IResource? _resource);
    //        return new AsyncResult<IResource>() { Success = success, Result = _resource };
    //    });
    //}

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

//public class AsyncResult<T>
//{
//    public AsyncResult()
//    { }

//    public AsyncResult(bool success, T? result)
//    {
//        Success = success;
//        Result = result;
//    }

//    public bool Success { get; set; }

//    public T? Result { get; set; }
//}
