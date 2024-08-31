using Symptum.Core.Extensions;
using Symptum.Core.Helpers;
using Symptum.Core.Management.Resources;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

public class ResourceHelper
{
    private static StorageFolder? _workFolder;

    public static StorageFolder? WorkFolder
    {
        get => _workFolder;
        private set
        {
            _workFolder = value;
            WorkFolderChanged?.Invoke(null, _workFolder);
        }
    }

    #region Work Folder Handling

    public static async Task<bool> OpenWorkFolderAsync(StorageFolder? folder = null)
    {
        bool result = await SelectWorkFolderAsync(folder);
        if (result && StorageHelper.IsFolderPickerSupported)
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
        ResourceManager.Resources.Clear();
    }

    private static async Task<bool> VerifyWorkFolderAsync(StorageFolder? targetFolder = null)
    {
        bool pathExists = true;
        if (targetFolder == null)
        {
            if (_workFolder == null)
                pathExists = await SelectWorkFolderAsync();
        }

        return pathExists;
    }

    public static async Task<bool> SelectWorkFolderAsync(StorageFolder? folder = null)
    {
        if (folder == null && StorageHelper.IsFolderPickerSupported)
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WindowHelper.WindowHandle);
#endif
            folder = await folderPicker.PickSingleFolderAsync();
        }

        if (folder != null && _workFolder != folder && _workFolder?.Path != folder.Path)
        {
            WorkFolder = folder;
            return true;
        }

        return false;
    }

    public static async Task<IReadOnlyList<StorageFile>?> GetFilesFromWorkPathAsync()
    {
        if (_workFolder != null)
        {
            return await _workFolder.GetFilesAsync();
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

    public static async Task<IResource?> LoadResourceFromFileAsync(StorageFile file, IResource? parent = null)
    {
        if (file.FileType.Equals(CsvFileExtension, StringComparison.InvariantCultureIgnoreCase))
            return await LoadCsvFileResourceFromFileAsync(file, parent);
        else if (file != null && file.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
            return await LoadPackageResourceFromFileAsync(file);

        return null;
    }

    private static async Task<CsvFileResource?> LoadCsvFileResourceFromFileAsync(StorageFile? file, IResource? parent)
    {
        if (file != null && file.FileType.Equals(CsvFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            string csv = await FileIO.ReadTextAsync(file);
            if (CsvResourceHelper.TryGetCsvResourceType(csv, out Type? csvType) &&
                Activator.CreateInstance(csvType) is CsvFileResource csvFileResource)
            {
                csvFileResource.Title = file.DisplayName;
                ResourceManager.LoadResourceFile(csvFileResource, csv);

                if (parent != null && parent.CanAddChildResourceType(csvType))
                    parent.AddChildResource(csvFileResource);
                else
                    ResourceManager.Resources.Add(csvFileResource);

                return csvFileResource;
            }
        }

        return null;
    }

    internal static async Task<PackageResource?> LoadPackageResourceFromFileAsync(StorageFile? file)
    {
        if (file != null && file.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            string json = await FileIO.ReadTextAsync(file);
            var package = ResourceManager.LoadPackageFromMetadata(json);
            if (package != null)
            {
                ResourceManager.Resources.Add(package);
                ResourceManager.RegisterResource(package);
                await LoadResourceAsync(package);
                return package;
            }
        }

        return null;
    }

    public static async Task LoadResourceAsync(IResource resource, IResource? parent = null)
    {
        if (resource is CsvFileResource csvResource)
        {
            await LoadCSVFileResourceAsync(csvResource);
            resource.InitializeResource(parent);
        }
        else
        {
            if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
            {
                await LoadMetadataResourceAsync(metadataResource);
            }

            resource.InitializeResource(parent);
            await LoadChildrenResourcesAsync(resource); // Temporary
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

    private static async Task LoadCSVFileResourceAsync(CsvFileResource csvResource)
    {
        StorageFile? csvFile = null;

        (string folderPath, string fileName, string extension) = GetDetailsFromFilePath(csvResource.FilePath);
        StorageFolder? folder = await StorageHelper.GetSubFolderAsync(_workFolder, folderPath);
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
            string text = await FileIO.ReadTextAsync(csvFile);
            ResourceManager.LoadResourceFile(csvResource, text);
        }
    }

    private static async Task LoadMetadataResourceAsync(MetadataResource resource)
    {
        StorageFile? jsonFile = null;

        (string folderPath, string fileName, string extension) = GetDetailsFromFilePath(resource.MetadataPath);
        StorageFolder? folder = await StorageHelper.GetSubFolderAsync(_workFolder, folderPath);
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
            string text = await FileIO.ReadTextAsync(jsonFile);
            ResourceManager.LoadResourceMetadata(resource, text);
        }
    }

    #endregion

    #region Saving Resources

    public static async Task<bool> SaveAllResourcesAsync(StorageFolder? targetFolder = null)
    {
        if (ResourceManager.Resources.Count > 0)
        {
            bool allSaved = true;
            foreach (var resource in ResourceManager.Resources)
            {
                allSaved &= await SaveResourceAsync(resource, targetFolder);
            }
            return allSaved;
        }

        return false;
    }

    public static async Task<bool> SaveResourceAsync(IResource resource, StorageFolder? targetFolder = null)
    {
        if (resource == null) return false;

        if (resource is CsvFileResource csvResource)
        {
            return await SaveCSVFileAsync(csvResource, targetFolder);
        }
        else
        {
            bool result = await SaveChildrenAsync(resource, targetFolder);

            if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
            {
                result &= await SaveMetadataAsync(metadataResource, targetFolder);
            }
            else if (resource is PackageResource package)
            {
                result &= await SaveMetadataAsync(package, targetFolder);
            }

            return result;
        }
    }

    private static async Task<bool> SaveCSVFileAsync(CsvFileResource csvResource, StorageFolder? targetFolder = null)
    {
        if (csvResource == null) return false;
        //bool pathExists = await VerifyWorkFolderAsync(targetFolder);
        //if (!pathExists) return false;

        string subFolderPath = ResourceManager.GetResourceFolderPath(csvResource);
        string? fileName = ResourceManager.GetResourceFileName(csvResource);
        csvResource.FilePath = subFolderPath + fileName + CsvFileExtension;
        StorageFile? saveFile = await PickSaveFileAsync(fileName, CsvFileExtension, "CSV File", targetFolder, subFolderPath);

        if (saveFile != null)
        {
            string? text = ResourceManager.WriteResourceFileText(csvResource);
            return await StorageHelper.WriteToFileAsync(saveFile, text);
        }

        return false;
    }

    private static async Task<bool> SaveMetadataAsync<T>(T resource, StorageFolder? targetFolder = null) where T : MetadataResource
    {
        if (resource == null) return false;
        //bool pathExists = await VerifyWorkFolderAsync(targetFolder);
        //if (!pathExists) return false;

        string subFolderPath = ResourceManager.GetResourceFolderPath(resource);
        string? fileName = ResourceManager.GetResourceFileName(resource);
        resource.MetadataPath = subFolderPath + fileName + JsonFileExtension;
        StorageFile? saveFile = await PickSaveFileAsync(fileName, JsonFileExtension, "JSON File", targetFolder, subFolderPath);

        if (saveFile != null)
        {
            string? text = resource is PackageResource package ?
                ResourceManager.WritePackageMetadata(package) : ResourceManager.WriteResourceMetadata(resource);
            return await StorageHelper.WriteToFileAsync(saveFile, text);
        }

        return false;
    }

    private static async Task<bool> SaveChildrenAsync(IResource? resource, StorageFolder? targetFolder = null)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            bool allChildrenSaved = true;
            foreach (var child in resource.ChildrenResources)
            {
                allChildrenSaved &= await SaveResourceAsync(child, targetFolder);
            }
            return allChildrenSaved;
        }
        return true;
    }

    private static async Task<StorageFile?> PickSaveFileAsync(string name, string extension, string fileType, StorageFolder? targetFolder = null, string? subFolder = null)
    {
        StorageFile saveFile;
        targetFolder ??= _workFolder;
        if (targetFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            var folder = await StorageHelper.CreateSubFoldersAsync(targetFolder, subFolder);
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
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, WindowHelper.WindowHandle);
#endif
            saveFile = await fileSavePicker.PickSaveFileAsync();
        }
        return saveFile;
    }

    #endregion

    #region Removing Resources

    public static async Task RemoveResourceAsync(IResource? resource, bool delete = false)
    {
        if (resource == null) return;

        await RemoveChildrenAsync(resource, delete);

        if (delete && resource is CsvFileResource csvResource)
            await DeleteCSVFileAsync(csvResource);
        else if (delete && resource is MetadataResource metadataResource)
            await DeleteMetadataAsync(metadataResource);

        IResource? parent = resource.ParentResource;
        if (parent != null)
            parent.RemoveChildResource(resource);
        else
        {
            ResourceManager.Resources.RemoveItemFromListIfExists(resource);
        }
        ResourceManager.UnregisterResource(resource);
    }

    private static async Task DeleteCSVFileAsync(CsvFileResource? csvResource)
    {
        if (csvResource == null) return;
        if (_workFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            try
            {
                string path = ResourceManager.GetResourceFolderPath(csvResource);
                var folder = await StorageHelper.GetSubFolderAsync(_workFolder, path);
                IStorageItem? file = await folder?.TryGetItemAsync(ResourceManager.GetResourceFileName(csvResource) + CsvFileExtension);
                if (file != null) await file.DeleteAsync();
            }
            catch { }
        }
    }

    private static async Task DeleteMetadataAsync(MetadataResource? resource)
    {
        if (resource == null) return;
        if (_workFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            try
            {
                string path = ResourceManager.GetResourceFolderPath(resource);
                string? resourceName = ResourceManager.GetResourceFileName(resource);

                var folder = await StorageHelper.GetSubFolderAsync(_workFolder, path);
                if (resource.SplitMetadata || resource is PackageResource)
                {
                    IStorageItem? file = await folder?.TryGetItemAsync(resourceName + JsonFileExtension);
                    if (file != null) await file.DeleteAsync();
                }
                IStorageItem? folder1 = await folder?.TryGetItemAsync(resourceName);
                if (folder1 != null) await folder1.DeleteAsync();
            }
            catch { }
        }
    }

    private static async Task RemoveChildrenAsync(IResource? resource, bool delete = false)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            var children = resource.ChildrenResources.ToList();
            foreach (var child in children)
            {
                await RemoveResourceAsync(child, delete);
            }
        }
    }

    #endregion

    public static event EventHandler<StorageFolder?> WorkFolderChanged;
}
