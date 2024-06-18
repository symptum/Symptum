using Symptum.Core.Extensions;
using static Symptum.Core.Helpers.FileHelper;
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
        private set
        {
            workFolder = value;
            WorkFolderChanged?.Invoke(null, workFolder);
        }
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

    #region Work Folder Handling

    public static async Task<bool> OpenWorkFolderAsync(StorageFolder? folder = null)
    {
        bool result = await SelectWorkFolderAsync(folder);
        if (result && _folderPicked)
        {
            ResourceManager.Resources.Clear();
            await LoadResourcesFromWorkPathAsync();
            return true;
        }

        return false;
    }

    public static void CloseWorkFolder()
    {
        WorkFolder = null;
        _folderPicked = false;
        ResourceManager.Resources.Clear();
    }

    private static async Task<bool> VerifyWorkFolderAsync()
    {
        bool pathExists = true;
        if (workFolder == null)
            pathExists = await SelectWorkFolderAsync();

        return pathExists;
    }

    private static async Task<bool> SelectWorkFolderAsync(StorageFolder? folder = null)
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
            WorkFolder = folder;
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
        if (file.FileType.Equals(CsvFileExtension, StringComparison.InvariantCultureIgnoreCase))
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
        else if (file.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            string json = await FileIO.ReadTextAsync(file);
            var package = ResourceManager.LoadPackageMetadata(json);
            if (package != null)
            {
                ResourceManager.Resources.Add(package);
                await LoadResourceAsync(package);
            }
        }
    }

    private static async Task LoadResourceAsync(IResource resource, IResource? parent = null)
    {
        if (resource is CsvFileResource csvResource)
            await LoadCSVFileResourceAsync(csvResource);
        else
        {
            if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
            {
                await LoadMetadataResourceAsync(metadataResource);
            }

            resource.InitializeResource(parent);
            resource.ActivateResource(); // Temporary
            await LoadChildrenResourcesAsync(resource);
        }
    }

    private static async Task LoadChildrenResourcesAsync(IResource resource)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            foreach (var child in resource.ChildrenResources)
            {
                await LoadResourceAsync(child, resource);
            }
        }
    }

    public static async Task LoadCSVFileResourceAsync(CsvFileResource csvResource)
    {
        StorageFile? csvFile = null;

        (string folderPath, string fileName, string extension) = ResourceManager.GetDetailsFromFilePath(csvResource.FilePath);
        StorageFolder? folder = await GetSubFolderAsync(workFolder, folderPath);
        if (folder != null)
        {
            try
            {
                csvFile = await folder.GetFileAsync(fileName + extension);
            }
            catch { }
        }

        if (csvFile != null)
        {
            string content = await FileIO.ReadTextAsync(csvFile);
            ResourceManager.LoadResourceFile(csvResource, content);
        }
    }

    public static async Task LoadMetadataResourceAsync(MetadataResource resource)
    {
        StorageFile? jsonFile = null;

        (string folderPath, string fileName, string extension) = ResourceManager.GetDetailsFromFilePath(resource.MetadataPath);
        StorageFolder? folder = await GetSubFolderAsync(workFolder, folderPath);
        if (folder != null)
        {
            try
            {
                jsonFile = await folder.GetFileAsync(fileName + extension);
            }
            catch { }
        }

        if (jsonFile != null)
        {
            string content = await FileIO.ReadTextAsync(jsonFile);
            ResourceManager.LoadResourceMetadata(resource, content);
        }
    }

    #endregion

    #region Saving Resources

    public static async Task<bool> SaveAllResourcesAsync()
    {
        if (ResourceManager.Resources.Count > 0 && await VerifyWorkFolderAsync())
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
        bool pathExists = await VerifyWorkFolderAsync();
        if (!pathExists) return false;

        if (resource is CsvFileResource csvResource)
        {
            return await SaveCSVFileAsync(csvResource);
        }
        else if (resource is PackageResource package)
        {
            bool result = true;
            result &= await SaveChildrenAsync(package);
            result &= await SaveMetadataAsync(package);
            return result;
        }
        else if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
        {
            bool result = true;
            result &= await SaveChildrenAsync(metadataResource);
            result &= await SaveMetadataAsync(metadataResource);
            return result;
        }

        return await SaveChildrenAsync(resource);
    }

    public static async Task<bool> SaveCSVFileAsync(CsvFileResource csvResource)
    {
        if (csvResource == null) return false;
        bool pathExists = await VerifyWorkFolderAsync();
        if (!pathExists) return false;

        string folder = ResourceManager.GetResourceFolderPath(csvResource.ParentResource);
        string? fileName = ResourceManager.GetResourceFileName(csvResource);
        csvResource.FilePath = folder + fileName + CsvFileExtension;
        StorageFile saveFile = await PickSaveFileAsync(fileName, CsvFileExtension, "CSV File", folder);

        if (saveFile != null)
        {
            string? content = ResourceManager.WriteResourceFile(csvResource);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    public static async Task<bool> SaveMetadataAsync<T>(T resource) where T : MetadataResource
    {
        if (resource == null) return false;
        bool pathExists = await VerifyWorkFolderAsync();
        if (!pathExists) return false;

        string path = ResourceManager.GetResourceFolderPath(resource.ParentResource);
        StorageFile saveFile = await PickSaveFileAsync(ResourceManager.GetResourceFileName(resource), JsonFileExtension, "JSON File", path);

        if (saveFile != null)
        {
            string content = resource is PackageResource package ?
                ResourceManager.WritePackageMetadata(package) : ResourceManager.WriteResourceMetadata(resource);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    private static async Task<bool> SaveChildrenAsync(IResource? resource)
    {
        if (resource != null && resource.CanHandleChildren && resource?.ChildrenResources != null)
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

    private static async Task<StorageFile?> PickSaveFileAsync(string name, string extension, string fileType, string? subFolder = null)
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

    #region Deleting Resources

    public static async Task DeleteResourceAsync(IResource? resource)
    {
        if (resource == null) return;

        if (resource is CsvFileResource csvResource)
        {
            await DeleteCSVFileAsync(csvResource);
        }
        else if (resource is PackageResource package)
        {
            await DeleteChildrenAsync(package);
            await DeleteMetadataAsync(package);
        }
        else if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
        {
            await DeleteChildrenAsync(metadataResource);
            await DeleteMetadataAsync(metadataResource);
        }
        else
        {
            await DeleteChildrenAsync(resource);
        }

        IResource? parent = resource.ParentResource;
        if (parent != null)
            parent.RemoveChildResource(resource);
        else
            ResourceManager.Resources.RemoveItemFromListIfExists(resource);
    }

    public static async Task DeleteCSVFileAsync(CsvFileResource? csvResource)
    {
        if (csvResource == null) return;
        if (_folderPicked && workFolder != null)
        {
            try
            {
                string path = ResourceManager.GetResourceFolderPath(csvResource.ParentResource);
                var folder = await GetSubFolderAsync(workFolder, path);
                IStorageItem? file = await folder?.TryGetItemAsync(ResourceManager.GetResourceFileName(csvResource) + CsvFileExtension);
                if (file != null) await file.DeleteAsync();
            }
            catch { }
        }
    }

    public static async Task DeleteMetadataAsync(MetadataResource? resource)
    {
        if (resource == null) return;
        if (_folderPicked && workFolder != null)
        {
            try
            {
                string path = ResourceManager.GetResourceFolderPath(resource.ParentResource);
                string? packageName = ResourceManager.GetResourceFileName(resource);

                var folder = await GetSubFolderAsync(workFolder, path);
                IStorageItem? file = await folder?.TryGetItemAsync(packageName + JsonFileExtension);
                if (file != null) await file.DeleteAsync();
                IStorageItem? folder1 = await folder?.TryGetItemAsync(packageName);
                if (folder1 != null) await folder1.DeleteAsync();
            }
            catch { }
        }
    }

    private static async Task DeleteChildrenAsync(IResource? resource)
    {
        if (resource != null && resource.ChildrenResources != null)
        {
            var children = resource.ChildrenResources.ToList();
            foreach (var child in children)
            {
                await DeleteResourceAsync(child);
            }
        }
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

    #endregion

    public static event EventHandler<StorageFolder?> WorkFolderChanged;
}
