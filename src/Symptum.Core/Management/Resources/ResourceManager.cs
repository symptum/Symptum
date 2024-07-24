using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Symptum.Core.Helpers.FileHelper;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Symptum.Core.Management.Resources;

public class ResourceManager
{
    private static readonly string defaultUriScheme = "symptum://";

    public static readonly Uri DefaultUri = new(defaultUriScheme);

    private static readonly ObservableCollection<IResource> _resources = [];

    public static ObservableCollection<IResource> Resources { get => _resources; }

    public static Uri GetAbsoluteUri(string path) => new(defaultUriScheme + path);

    #region Dependency Resolution

    private static readonly Dictionary<string, List<TaskCompletionSource<IResource?>>> dependencyLinks = [];

    // TODO: Make thread safe
    public static void ResolveDependencies(IResource? resource)
    {
        if (resource != null && resource.DependencyIds != null)
        {
            ObservableCollection<IResource>? dependencies = [];
            resource.Dependencies = dependencies;
            foreach (var dependencyId in resource.DependencyIds)
            {
                ResolveDependencyAsync(resource, dependencyId);
            }
        }
    }

    private static async void ResolveDependencyAsync(IResource? resource, string dependencyId)
    {
        IResource? dependency = await GetDependencyAsync(dependencyId);
        if (dependency != null)
            resource?.Dependencies?.Add(dependency);
    }

    private static async Task<IResource?> GetDependencyAsync(string id)
    {
        TaskCompletionSource<IResource?> taskCompletionSource = new();
        if (!dependencyLinks.TryGetValue(id, out List<TaskCompletionSource<IResource?>>? tasks))
        {
            tasks = [];
            dependencyLinks.Add(id, tasks);
        }

        tasks.Add(taskCompletionSource);
        var resource = await taskCompletionSource.Task;
        return resource;
    }

    private static int resolutions = 0;

    // Called first after loading all the local resources.
    // Then again after all the primary dependencies have been loaded.
    // Then so on after loading further dependencies.
    public static void StartDependencyResolution()
    {
        resolutions++;
        List<string> finishedIds = [];
        foreach (var pair in dependencyLinks)
        {
            var id = pair.Key;
            var tasks = pair.Value;
            IResource? dependency = Resources.FirstOrDefault(x => x.Id == id);
            if (dependency != null)
            {
                foreach (var task in tasks)
                {
                    task.SetResult(dependency);
                }
                tasks.Clear();
                finishedIds.Add(id);
            }
            else
            {
                // Load Dependency?
                LoadDependencyAsync(id);
            }
        }
        foreach (var id in finishedIds)
        {
            dependencyLinks.Remove(id);
        }
        finishedIds.Clear();
        //System.Diagnostics.Debug.WriteLine("No. of resolutions: " + resolutions);
        //foreach (var resource in resources)
        //{
        //    string output = $"{resource.Title}[{resource.Id}] depends on: ";
        //    foreach (var dependency in resource.Dependencies)
        //    {
        //        output += $"{dependency.Title}[{dependency.Id}],";
        //    }
        //    System.Diagnostics.Debug.WriteLine(output);
        //}
    }

    // idk what or how this works, made sense to me
    private static int loadWaits = 0;

    private static async void LoadDependencyAsync(string id)
    {
        loadWaits++;
        // Ask PackageManager to fetch (from remote or cache) the package.
        var package = await PackageManager.LoadPackageAsync(id);
        if (package != null)
        {
            Resources.Add(package);
            // Link the newly loaded dependencies
            if (dependencyLinks.TryGetValue(id, out var tasks))
            {
                foreach (var task in tasks)
                {
                    task.SetResult(package);
                }
                dependencyLinks.Remove(id);
            }
            ResolveDependencies(package);

            loadWaits--;
        }
        if (loadWaits == 0)
        {
            StartDependencyResolution();
            _ = id;
        }
    }

    #endregion

    #region Resource File Handling

    public static string? GetResourceFileName(IResource? resource) => resource?.Title;

    public static (string folder, string fileName, string extension) GetDetailsFromFilePath(string? filePath)
    {
        string folder = string.Empty;
        string fileName = string.Empty;
        string extension = string.Empty;

        if (filePath == null) return (folder, fileName, extension);

        int dotIndex, slashIndex;
        dotIndex = slashIndex = filePath.Length;

        for (int i = filePath.Length - 1; i >= 0; i--)
        {
            char ch = filePath[i];
            if (ch == PathSeparator)
            {
                slashIndex = i + 1; // To include '\'
                break;
            }
            else if (ch == ExtensionSeparator)
            {
                dotIndex = i;
                continue;
            }
        }

        if (dotIndex > 0 && slashIndex > 0)
        {
            folder = filePath[..slashIndex];
            fileName = filePath[slashIndex..dotIndex];
            extension = filePath[dotIndex..];
        }

        return (folder, fileName, extension);
    }

    public static string GetResourceFolderPath(IResource? parent)
    {
        string _path = PathSeparator.ToString();
        if (parent != null)
        {
            _path = GetResourceFolderPath(parent.ParentResource) + GetResourceFileName(parent) + PathSeparator;
        }
        return _path;
    }

    public static string GetResourceFilePath(IResource? resource, string? extension)
    {
        string path = GetResourceFolderPath(resource?.ParentResource);
        return path + GetResourceFileName(resource) + extension;
    }

    public static void LoadResourceFile(FileResource? fileResource, string content) => fileResource?.ReadFileContent(content);

    public static string? WriteResourceFile(FileResource? fileResource) => fileResource?.WriteFileContent();

    public static PackageResource? LoadPackageMetadata(string metadata)
    {
        var package = JsonSerializer.Deserialize<PackageResource>(metadata);
        return package;
    }

    public static void LoadResourceMetadata(MetadataResource? resource, string metadata) => resource?.LoadMetadata(metadata);

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull
    };

    public static string WritePackageMetadata(PackageResource package) => JsonSerializer.Serialize(package, options);

    public static string WriteResourceMetadata<T>(T resource) where T : MetadataResource => JsonSerializer.Serialize(resource, resource.GetType(), options);

    #endregion

    #region Resource Fetching

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
