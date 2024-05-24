using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Windows.Foundation;
using Windows.Storage.Pickers;

namespace Symptum.Common.Helpers;

public class ResourceHelper
{
    private static XamlRoot? _xamlRoot;
    private static IntPtr _hWnd = IntPtr.Zero;

    private static StorageFolder? workFolder;

    public static StorageFolder? WorkFolder
    {
        get => workFolder;
    }

    private static bool _folderPicked = false;

    public static bool FolderPicked
    {
        get => _folderPicked;
    }

    public static void Initialize(XamlRoot? xamlRoot, IntPtr hWnd)
    {
        _xamlRoot = xamlRoot;
        _hWnd = hWnd;
    }

    #region Work Path Handling

    public static async Task<bool> OpenWorkPathAsync(StorageFolder? folder = null)
    {
        bool result = await SelectWorkPathAsync(folder);
        if (result && _folderPicked)
        {
            ResourceManager.Resources.Clear();
            await LoadResourcesFromWorkPathAsync();
            return true;
        }

        return false;
    }

    public static void CloseWorkPath()
    {
        workFolder = null;
        _folderPicked = false;
        ResourceManager.Resources.Clear();
    }

    private static async Task<bool> VerifyWorkPathAsync()
    {
        bool pathExists = true;
        if (workFolder == null)
            pathExists = await SelectWorkPathAsync();

        return pathExists;
    }

    private static async Task<bool> SelectWorkPathAsync(StorageFolder? folder = null)
    {
        if (folder == null)
        {
            if (StorageHelper.IsFolderPickerSupported)
            {
                FolderPicker folderPicker = new();
                folderPicker.FileTypeFilter.Add("*");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, _hWnd);
#endif
                folder = await folderPicker.PickSingleFolderAsync();
                _folderPicked = true;
            }
            else
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                folder = await localFolder.CreateFolderAsync("Temp", CreationCollisionOption.OpenIfExists);
                _folderPicked = false;
            }
        }

        if (folder != null && workFolder != folder && workFolder?.Path != folder.Path)
        {
            workFolder = folder;
            return true;
        }

        return false;
    }

    public static async Task<IReadOnlyList<StorageFile>?> GetFilesFromWorkPathAsync()
    {
        if (workFolder != null)
        {
            return await workFolder.GetFilesAsync();
        }

        return null;
    }

    #endregion

    #region Loading Resources

    public static async Task LoadResourcesFromWorkPathAsync()
    {
        var files = await GetFilesFromWorkPathAsync();
        await LoadResourcesFromFilesAsync(files);
    }

    public static async Task LoadResourcesFromFilesAsync(IEnumerable<StorageFile>? files, IResource? parent = null)
    {
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            await LoadResourceFromFileAsync(file, parent);
        }
    }

    private static async Task LoadResourceFromFileAsync(StorageFile file, IResource? parent = null)
    {
        if (file == null) return;
        if (file.FileType.Equals(".csv", StringComparison.InvariantCultureIgnoreCase))
        {
            string csv = await FileIO.ReadTextAsync(file);
            var topic = new QuestionBankTopic(file.DisplayName);
            ResourceManager.LoadResourceFile(topic, csv);
            if (topic != null)
            {
                if (parent != null && parent.CanAddChildResourceType(typeof(QuestionBankTopic)))
                    parent.AddChildResource(topic);
                else
                    ResourceManager.Resources.Add(topic);
            }
        }
        else if (file.FileType.Equals(".json", StringComparison.InvariantCultureIgnoreCase))
        {
            string json = await FileIO.ReadTextAsync(file);
            var package = ResourceManager.LoadPackageMetadata(json);
            if (package != null)
            {
                ResourceManager.Resources.Add(package);
                ((IResource)package).InitializeResource(null);
                await LoadChildrenResourcesAsync(package);
            }
        }
    }

    private static async Task LoadChildrenResourcesAsync(IResource resource)
    {
        if (resource is CsvFileResource csvResource)
            await LoadCSVFileResourceAsync(csvResource);
        else if (resource.ChildrenResources != null)
        {
            foreach (var child in resource.ChildrenResources)
            {
                await LoadChildrenResourcesAsync(child);
            }
        }
    }

    public static async Task LoadCSVFileResourceAsync(CsvFileResource csvResource)
    {
        StorageFile? csvFile = null;

        StorageFolder? folder = await GetSubFolderAsync(workFolder, csvResource.Path);
        if (folder != null)
        {
            try
            {
                csvFile = await folder.GetFileAsync(csvResource.Title + ".csv");
            }
            catch { }
        }

        if (csvFile != null)
        {
            string content = await FileIO.ReadTextAsync(csvFile);
            ResourceManager.LoadResourceFile(csvResource, content);
        }
    }

    #endregion

    #region Saving Resources

    public static async Task<bool> SaveAllResourcesAsync()
    {
        if (ResourceManager.Resources.Count > 0 && await VerifyWorkPathAsync())
        {
            bool allSaved = true;
            foreach (var resource in ResourceManager.Resources)
            {
                allSaved &= await SaveResourceAsync(resource);
            }
            return allSaved;
        }

        return false;
    }

    public static async Task<bool> SaveResourceAsync(IResource resource)
    {
        if (resource == null) return false;
        bool pathExists = await VerifyWorkPathAsync();
        if (!pathExists) return false;

        if (resource is CsvFileResource csvResource)
        {
            return await SaveCSVFileAsync(csvResource);
        }
        else if (resource is PackageResource package)
        {
            bool result = true;
            result &= await SaveChildrenAsync(package);
            result &= await SavePackageAsync(package);
            return result;
        }

        return await SaveChildrenAsync(resource);
    }

    public static async Task<bool> SaveCSVFileAsync(CsvFileResource csvResource)
    {
        if (csvResource == null) return false;
        bool pathExists = await VerifyWorkPathAsync();
        if (!pathExists) return false;

        csvResource.Path = GetPath(csvResource.ParentResource);
        StorageFile saveFile = await PickSaveFileAsync(csvResource.Title, ".csv", "CSV File", csvResource.Path);

        if (saveFile != null)
        {
            string content = ResourceManager.WriteResourceFile(csvResource);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    public static async Task<bool> SavePackageAsync(PackageResource package)
    {
        if (package == null) return false;
        bool pathExists = await VerifyWorkPathAsync();
        if (!pathExists) return false;

        StorageFile saveFile = await PickSaveFileAsync(package.Title, ".json", "JSON File");

        if (saveFile != null)
        {
            string content = ResourceManager.WritePackageMetadata(package);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    private static async Task<bool> SaveChildrenAsync(IResource? resource)
    {
        if (resource?.ChildrenResources != null)
        {
            bool allChildrenSaved = true;
            foreach (var child in resource.ChildrenResources)
            {
                allChildrenSaved &= await SaveResourceAsync(child);
            }
            return allChildrenSaved;
        }
        return true;
    }

    private static async Task<StorageFile> PickSaveFileAsync(string name, string extension, string fileType, string? subFolder = null)
    {
        StorageFile saveFile;
        if (_folderPicked && workFolder != null)
        {
            var folder = await CreateSubFoldersAsync(workFolder, subFolder);
            saveFile = await folder?.CreateFileAsync(name + extension, CreationCollisionOption.ReplaceExisting);
        }
        else
        {
            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = name
            };
            fileSavePicker.FileTypeChoices.Add(fileType, [extension]);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, _hWnd);
#endif
            saveFile = await fileSavePicker.PickSaveFileAsync();
        }
        return saveFile;
    }

    #endregion

    #region Storage Methods

    private static async Task<StorageFolder?> SubFolderFuncAsync(StorageFolder? parent, string? path, Func<StorageFolder?, string, IAsyncOperation<StorageFolder>> func)
    {
        if (path == null) return parent;

        StorageFolder? folder = parent;

        path = path.Trim(PathSeparator);
        var folders = path.Split(PathSeparator);

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrWhiteSpace(folderName))
            {
                try
                {
                    folder = await func(folder, folderName);
                }
                catch { return null; }
            }
        }

        return folder;
    }

    private static async Task<StorageFolder?> GetSubFolderAsync(StorageFolder? parent, string? path)
    {
        StorageFolder? folder = await SubFolderFuncAsync(parent, path,
            (f, name) => f?.GetFolderAsync(name));
        return folder;
    }

    private static async Task<StorageFolder?> CreateSubFoldersAsync(StorageFolder? parent, string? path = null)
    {
        StorageFolder? folder = await SubFolderFuncAsync(parent, path,
            (f, name) => f?.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists));
        return folder;
    }

    private static async Task<bool> WriteToFileAsync(StorageFile file, string content)
    {
        CachedFileManager.DeferUpdates(file);
        await FileIO.WriteTextAsync(file, content);
        await CachedFileManager.CompleteUpdatesAsync(file);
        return true;
    }

    private static string GetPath(IResource? parent)
    {
        string _path = PathSeparator.ToString();
        if (parent != null)
        {
            _path = GetPath(parent.ParentResource) + parent.Title + PathSeparator;
        }
        return _path;
    }

    #endregion

    #region Deleting Resources

    public static async Task<bool> DeleteResourceAsync(IResource? resource)
    {
        if (resource == null) return false;

        bool result;
        if (resource is CsvFileResource csvResource)
        {
            result = await DeleteCSVFileAsync(csvResource);
        }
        else if (resource is PackageResource package)
        {
            result = true;
            result &= await DeleteChildrenAsync(package);
            result &= await DeletePackageAsync(package);
        }
        else
        {
            result = await DeleteChildrenAsync(resource);
        }

        IResource? parent = resource.ParentResource;
        if (parent != null)
            parent.RemoveChildResource(resource);
        else
            ResourceManager.Resources.RemoveItemFromListIfExists(resource);

        return result;
    }

    public static async Task<bool> DeleteCSVFileAsync(CsvFileResource? csvResource)
    {
        if (csvResource == null) return false;
        if (_folderPicked && workFolder != null)
        {
            try
            {
                string path = GetPath(csvResource.ParentResource);
                var folder = await GetSubFolderAsync(workFolder, path);
                IStorageItem? file = await folder?.TryGetItemAsync(csvResource.Title + ".csv");
                if (file != null)
                {
                    await file.DeleteAsync();
                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    public static async Task<bool> DeletePackageAsync(PackageResource? package)
    {
        if (package == null) return false;
        if (_folderPicked && workFolder != null)
        {
            try
            {
                IStorageItem? file = await workFolder?.TryGetItemAsync(package.Title + ".json");
                if (file != null) await file.DeleteAsync();
                IStorageItem? folder = await workFolder?.TryGetItemAsync(package.Title);
                if (folder != null) await folder.DeleteAsync();
            }
            catch { }
        }

        return false;
    }

    private static async Task<bool> DeleteChildrenAsync(IResource? resource)
    {
        if (resource != null && resource.ChildrenResources != null)
        {
            bool allChildrenDeleted = true;
            var children = resource.ChildrenResources.ToList();
            foreach (var child in children)
            {
                allChildrenDeleted &= await DeleteResourceAsync(child);
            }
            return allChildrenDeleted;
        }
        return true;
    }

    #endregion

    private const char PathSeparator = '\\';
}
