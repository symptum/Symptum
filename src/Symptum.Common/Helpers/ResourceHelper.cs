using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Windows.Foundation;
using Windows.Storage.Pickers;

namespace Symptum.Common.Helpers;

public class ResourceHelper
{
    private static StorageFolder? workFolder;

    private static XamlRoot? _xamlRoot;
    private static IntPtr _hWnd = IntPtr.Zero;

    private static bool _folderPicked = false;

    public static bool FolderPicked
    {
        get => _folderPicked;
        set => _folderPicked = value;
    }

    public static void Initialize(XamlRoot? xamlRoot, IntPtr hWnd)
    {
        _xamlRoot = xamlRoot;
        _hWnd = hWnd;
    }

    public static void CloseWorkPath()
    {
        workFolder = null;
        _folderPicked = false;
        ResourceManager.Resources.Clear();
    }

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

    private static async Task<bool> VerifyWorkPathAsync()
    {
        bool pathExists = true;
        if (workFolder == null)
            pathExists = await SelectWorkPathAsync();

        return pathExists;
    }

    public static async Task<bool> SelectWorkPathAsync(StorageFolder? folder = null)
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
                catch { }
                if (folder == null) return parent;
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

    private static async Task<StorageFolder?> CreateSubFoldersAsync(StorageFolder? parent, string? path)
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

    private const char PathSeparator = '\\';
}
