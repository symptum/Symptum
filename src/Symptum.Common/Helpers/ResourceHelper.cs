using Symptum.Core.Extensions;
using Symptum.Core.Helpers;
using Symptum.Core.Management.Resources;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

public class ResourceHelper
{
    private static StorageFolder? _workFolder;

    public static StorageFolder? WorkFolder
    {
        get => _workFolder;
        internal set
        {
            _workFolder = value;
            WorkFolderChanged?.Invoke(null, _workFolder);
        }
    }

    #region Work Folder Handling

    public static void CloseWorkFolder()
    {
        WorkFolder = null;
        ResourceManager.Resources.Clear();
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

    #region Resource File Handling

    private static readonly Dictionary<FileResource, StorageFile> fileMap = [];

    public static async Task<IRandomAccessStream?> OpenFileForReadAsync(FileResource fileResource)
    {
        if (!string.IsNullOrEmpty(fileResource.FilePath) &&
            fileMap.TryGetValue(fileResource, out StorageFile? file))
        {
            return await file.OpenReadAsync();
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

    public static async Task LoadResourcesFromFilesAsync(IEnumerable<StorageFile>? files, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            await LoadResourceFromFileAsync(file, parent, sourceFolder);
        }
    }

    public static async Task<IResource?> LoadResourceFromFileAsync(StorageFile? file, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (file == null) return null;

        if (file.FileType.Equals(CsvFileExtension, StringComparison.InvariantCultureIgnoreCase))
            return await LoadCsvFileResourceFromFileAsync(file, parent);
        else if (file.FileType.Equals(MarkdownFileExtension, StringComparison.InvariantCultureIgnoreCase))
            return await LoadMarkdownFileResourceFromFileAsync(file, parent);
        else if (file.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
            return await LoadPackageResourceFromFileAsync(file, parent, sourceFolder);
        else if (ImageFileExtensions.Any(ext => ext.Equals(file.FileType, StringComparison.InvariantCultureIgnoreCase)))
            return await LoadImageFileResourceFromFileAsync(file, parent);
        else if (AudioFileExtensions.Any(ext => ext.Equals(file.FileType, StringComparison.InvariantCultureIgnoreCase)))
            return await LoadAudioFileResourceFromFileAsync(file, parent);

        return null;
    }

    private static async Task<CsvFileResource?> LoadCsvFileResourceFromFileAsync(StorageFile? file, IResource? parent)
    {
        if (file != null)
        {
            string csv = await FileIO.ReadTextAsync(file);
            if (CsvResourceHelper.TryGetCsvResourceType(csv, out Type? csvType) &&
                Activator.CreateInstance(csvType) is CsvFileResource csvFileResource)
            {
                csvFileResource.Title = file.DisplayName;
                ResourceManager.LoadResourceFileText(csvFileResource, csv);

                if (parent != null && parent.CanAddChildResourceType(csvType))
                    parent.AddChildResource(csvFileResource);
                else
                    ResourceManager.Resources.Add(csvFileResource);

                return csvFileResource;
            }
        }

        return null;
    }

    private static async Task<MarkdownFileResource?> LoadMarkdownFileResourceFromFileAsync(StorageFile? file, IResource? parent)
    {
        if (file != null)
        {
            string md = await FileIO.ReadTextAsync(file);

            MarkdownFileResource markdownFileResource = new()
            {
                Title = file.DisplayName
            };
            ResourceManager.LoadResourceFileText(markdownFileResource, md);

            if (parent != null && parent.CanAddChildResourceType(typeof(MarkdownFileResource)))
                parent.AddChildResource(markdownFileResource);
            else
                ResourceManager.Resources.Add(markdownFileResource);

            return markdownFileResource;
        }

        return null;
    }

    private static async Task<IResource?> LoadImageFileResourceFromFileAsync(StorageFile? file, IResource? parent)
    {
        if (file != null)
        {
            ImageFileResource imageFileResource = new()
            {
                Title = file.DisplayName,
                FilePath = file.Path
            };
            imageFileResource.SetMediaFileExtension(file.FileType.ToLower());

            fileMap.TryAdd(imageFileResource, file);

            if (parent != null && parent.CanAddChildResourceType(typeof(ImageFileResource)))
                parent.AddChildResource(imageFileResource);
            else
                ResourceManager.Resources.Add(imageFileResource);

            return imageFileResource;
        }

        return null;
    }

    private static async Task<IResource?> LoadAudioFileResourceFromFileAsync(StorageFile? file, IResource? parent)
    {
        if (file != null)
        {
            AudioFileResource audioFileResource = new()
            {
                Title = file.DisplayName,
                FilePath = file.Path
            };
            audioFileResource.SetMediaFileExtension(file.FileType.ToLower());

            fileMap.TryAdd(audioFileResource, file);

            if (parent != null && parent.CanAddChildResourceType(typeof(AudioFileResource)))
                parent.AddChildResource(audioFileResource);
            else
                ResourceManager.Resources.Add(audioFileResource);

            return audioFileResource;
        }

        return null;
    }

    internal static async Task<PackageResource?> LoadPackageResourceFromFileAsync(StorageFile? file, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (file != null)
        {
            string json = await FileIO.ReadTextAsync(file);
            var package = ResourceManager.LoadPackageFromMetadata(json);
            if (package != null)
            {
                if (parent != null && parent.CanAddChildResourceType(package.GetType()))
                    parent.AddChildResource(package);
                else
                    ResourceManager.Resources.Add(package);
                ResourceManager.RegisterResource(package);
                await LoadResourceAsync(package, parent, sourceFolder);
                return package;
            }
        }

        return null;
    }

    public static async Task LoadResourceAsync(IResource? resource, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (resource is TextFileResource textResource)
        {
            // CSVs and Markdowns
            await LoadTextFileResourceAsync(textResource, sourceFolder);
            resource.InitializeResource(parent);
        }
        else if (resource is MediaFileResource mediaResource)
        {
            // Images and Audios
            await LoadMediaFileResourceAsync(mediaResource, sourceFolder);
            resource.InitializeResource(parent);
        }
        else
        {
            if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata)
            {
                await LoadMetadataResourceAsync(metadataResource, sourceFolder);
            }

            resource.InitializeResource(parent);
            await LoadChildrenResourcesAsync(resource, sourceFolder); // Temporary
        }
    }

    private static async Task LoadChildrenResourcesAsync(IResource? resource, StorageFolder? sourceFolder = null)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            foreach (var child in resource.ChildrenResources)
            {
                await LoadResourceAsync(child, resource, sourceFolder);
            }
        }
    }

    private static async Task<StorageFile?> GetResourceFileAsync(string? path, StorageFolder? sourceFolder = null)
    {
        sourceFolder ??= _workFolder;
        (string folderPath, string fileName, string extension) = GetDetailsFromFilePath(path);
        StorageFolder? folder = await StorageHelper.GetSubFolderAsync(sourceFolder, folderPath);
        if (folder != null)
        {
            try
            {
                return await folder.GetFileAsync(fileName + extension);
            }
            catch { }
        }

        return null;
    }

    private static async Task LoadTextFileResourceAsync(TextFileResource textResource, StorageFolder? sourceFolder = null)
    {
        if (await GetResourceFileAsync(textResource.FilePath, sourceFolder) is StorageFile textFile)
        {
            string text = await FileIO.ReadTextAsync(textFile);
            ResourceManager.LoadResourceFileText(textResource, text);
        }
    }

    private static async Task LoadMediaFileResourceAsync(MediaFileResource mediaResource, StorageFolder? sourceFolder = null)
    {
        if (await GetResourceFileAsync(mediaResource.FilePath, sourceFolder) is StorageFile mediaFile)
        {
            mediaResource.SetMediaFileExtension(mediaFile.FileType.ToLower());
            fileMap.TryAdd(mediaResource, mediaFile);
        }
    }

    private static async Task LoadMetadataResourceAsync(MetadataResource resource, StorageFolder? sourceFolder = null)
    {
        if (await GetResourceFileAsync(resource.MetadataPath, sourceFolder) is StorageFile jsonFile)
        {
            string text = await FileIO.ReadTextAsync(jsonFile);
            ResourceManager.LoadResourceMetadata(resource, text);
        }
    }

    #endregion

    #region Saving Resources

    public static async Task<bool> SaveResourceAsync(IResource? resource, StorageFolder? targetFolder = null, bool saveChildren = true)
    {
        if (resource == null) return false;

        if (resource is TextFileResource textResource)
        {
            // CSVs and Markdowns
            return await SaveTextFileResourceAsync(textResource, targetFolder);
        }
        else if (resource is MediaFileResource mediaResource)
        {
            // Images and Audios
            return await CopySaveFileResourceAsync(mediaResource, targetFolder);
        }
        else
        {
            bool result = !saveChildren || await SaveChildrenAsync(resource, targetFolder);

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

    private static async Task<bool> SaveTextFileResourceAsync(TextFileResource textResource, StorageFolder? targetFolder = null)
    {
        if (textResource == null) return false;

        string subFolderPath = ResourceManager.GetRelativeResourceFolderPath(textResource);
        string? fileName = ResourceManager.GetResourceFileName(textResource);
        textResource.FilePath = subFolderPath + fileName + textResource.FileExtension;
        StorageFile? saveFile = await PickSaveFileAsync(fileName, textResource.FileExtension, $"{textResource.FileType} File", targetFolder, subFolderPath);

        if (saveFile != null)
        {
            string? text = ResourceManager.WriteResourceFileText(textResource);
            if (text != null)
                return await StorageHelper.WriteToFileAsync(saveFile, text);
        }

        return false;
    }

    private static async Task<bool> CopySaveFileResourceAsync(FileResource fileResource, StorageFolder? targetFolder = null)
    {
        if (fileResource == null) return false;

        string subFolderPath = ResourceManager.GetRelativeResourceFolderPath(fileResource);
        string? fileName = ResourceManager.GetResourceFileName(fileResource);
        string filePath = subFolderPath + fileName + fileResource.FileExtension;

        if (filePath.Equals(fileResource.FilePath, StringComparison.InvariantCultureIgnoreCase) &&
            (targetFolder == null || targetFolder == _workFolder)) // Prevent writing the file onto itself.
            return true;

        if (fileMap.TryGetValue(fileResource, out var file))
        {
            fileResource.FilePath = filePath;

            StorageFile? saveFile = await CopyFileAsync(file, fileName, fileResource.FileExtension, $"{fileResource.FileType} File", targetFolder, subFolderPath);
            fileMap.Remove(fileResource);
            if (saveFile != null) fileMap.TryAdd(fileResource, saveFile);
        }

        return true;
    }

    private static async Task<bool> SaveMetadataAsync<T>(T resource, StorageFolder? targetFolder = null) where T : MetadataResource
    {
        if (resource == null) return false;

        string subFolderPath = ResourceManager.GetRelativeResourceFolderPath(resource);
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

    internal static async Task<StorageFile?> PickSaveFileAsync(string? name, string? extension, string? fileType, StorageFolder? targetFolder = null, string? subFolder = null)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(extension) ||
            string.IsNullOrEmpty(fileType)) return null;

        StorageFile? saveFile = null;
        targetFolder ??= _workFolder;
        if (targetFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            var folder = await StorageHelper.CreateSubFoldersAsync(targetFolder, subFolder);
            if (folder != null) saveFile = await folder.CreateFileAsync(name + extension, CreationCollisionOption.ReplaceExisting);
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

    private static async Task<StorageFile?> CopyFileAsync(StorageFile? file, string? name, string? extension, string? fileType, StorageFolder? targetFolder = null, string? subFolder = null)
    {
        StorageFile? saveFile = file;
        targetFolder ??= _workFolder;

        if (file == null || string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(extension) ||
            string.IsNullOrEmpty(fileType)) return saveFile;

        if (targetFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            var folder = await StorageHelper.CreateSubFoldersAsync(targetFolder, subFolder);
            try
            {
                // NOTE: instead of doing this check here, do it before calling this function.
                // This is to prevent copying the file onto itself.

                //StorageFolder parent = await file.GetParentAsync();
                //if (parent != null && folder != null && !parent.Path.Equals(folder?.Path, StringComparison.InvariantCultureIgnoreCase))
                if (folder != null) saveFile = await file.CopyAsync(folder, name + extension, NameCollisionOption.ReplaceExisting);
            }
            catch { }
        }
        else
        {
            saveFile = await PickSaveFileAsync(name, extension, fileType, targetFolder, subFolder);
            if (saveFile != null)
            {
                CachedFileManager.DeferUpdates(saveFile);
                var source = await file.OpenStreamForReadAsync();
                var destination = await saveFile.OpenStreamForWriteAsync();
                await source.CopyToAsync(destination);
                await source.FlushAsync();
                await destination.FlushAsync();
                await CachedFileManager.CompleteUpdatesAsync(saveFile);
            }
        }

        return saveFile;
    }

    #endregion

    #region Removing Resources

    public static async Task RemoveResourceAsync(IResource? resource, bool delete = false)
    {
        if (resource == null) return;

        await RemoveChildrenAsync(resource, delete);

        if (resource is FileResource fileResource)
        {
            if (fileResource is MediaFileResource)
                fileMap.Remove(fileResource); // Images and Audios
            if (delete) await DeleteResourceFileAsync(fileResource);
        }
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

    private static async Task DeleteResourceFileAsync(FileResource? fileResource)
    {
        if (fileResource == null) return;
        if (_workFolder != null && StorageHelper.IsFolderPickerSupported)
        {
            try
            {
                string path = ResourceManager.GetAbsoluteResourceFolderPath(fileResource);
                var folder = await StorageHelper.GetSubFolderAsync(_workFolder, path);
                if (folder == null) return;
                IStorageItem? file = await folder.TryGetItemAsync(ResourceManager.GetResourceFileName(fileResource) + fileResource.FileExtension);
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
                string path = ResourceManager.GetAbsoluteResourceFolderPath(resource);
                string? resourceName = ResourceManager.GetResourceFileName(resource);

                var folder = await StorageHelper.GetSubFolderAsync(_workFolder, path);
                if (folder == null) return;
                if (resource.SplitMetadata || resource is PackageResource)
                {
                    IStorageItem? file = await folder.TryGetItemAsync(resourceName + JsonFileExtension);
                    if (file != null) await file.DeleteAsync();
                }
                IStorageItem? folder1 = await folder.TryGetItemAsync(resourceName);
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
