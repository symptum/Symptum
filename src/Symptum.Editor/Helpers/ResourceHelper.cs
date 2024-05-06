using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Windows.Storage.Pickers;

namespace Symptum.Editor.Helpers;

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

    public static async Task<IReadOnlyList<StorageFile>?> GetFilesFromWorkPathAsync()
    {
        if (workFolder != null)
        {
            return await workFolder.GetFilesAsync();
        }

        return null;
    }

    public static async Task<bool> VerifyWorkPathAsync()
    {
        bool pathExists;
        if (workFolder == null)
            pathExists = await SelectWorkPathAsync();
        else pathExists = true;

        return pathExists;
    }

    public static async Task<bool> SelectWorkPathAsync()
    {
        StorageFolder folder;
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

        if (folder != null && workFolder != folder)
        {
            workFolder = folder;
            return true;
        }

        return false;
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
        if (file.FileType.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
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
        else if (file.FileType.Equals(".json", StringComparison.CurrentCultureIgnoreCase))
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

    private static async Task<StorageFolder?> GetSubFolderAsync(StorageFolder? parent, string? path)
    {
        if (path == null) return parent;

        StorageFolder? folder = parent;

        path = path.Trim('\\');
        var folders = path.Split('\\');

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrWhiteSpace(folderName))
            {
                try
                {
                    folder = await folder?.GetFolderAsync(folderName);
                }
                catch { }
                if (folder == null) break;
            }
        }

        return folder;
    }

    private static async Task<bool> SaveChildrenAsync(IResource resource)
    {
        if (resource.ChildrenResources != null)
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

    public static async Task<bool> SaveResourceAsync(IResource resource)
    {
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

    private static async Task<bool> WriteToFileAsync(StorageFile file, string content)
    {
        CachedFileManager.DeferUpdates(file);
        await FileIO.WriteTextAsync(file, content);
        await CachedFileManager.CompleteUpdatesAsync(file);
        return true;
    }

    private static async Task<StorageFolder?> CreateSubFoldersAsync(StorageFolder? parent, string? path)
    {
        if (path == null) return parent;

        StorageFolder? folder = parent;

        path = path.Trim('\\');
        var folders = path.Split('\\');

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrEmpty(folderName) && !string.IsNullOrWhiteSpace(folderName))
                folder = await folder?.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
        }

        return folder;
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

    public static async Task<bool> SavePackageAsync(PackageResource package)
    {
        if (package == null) return false;

        StorageFile saveFile = await PickSaveFileAsync(package.Title, ".json", "JSON File");

        if (saveFile != null)
        {
            string content = ResourceManager.WritePackageMetadata(package);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    public static async Task<bool> SaveCSVFileAsync(CsvFileResource csvResource)
    {
        if (csvResource == null) return false;

        csvResource.Path = GetPath(csvResource.ParentResource);
        StorageFile saveFile = await PickSaveFileAsync(csvResource.Title, ".csv", "CSV File", csvResource.Path);

        if (saveFile != null)
        {
            string content = ResourceManager.WriteResourceFile(csvResource);
            return await WriteToFileAsync(saveFile, content);
        }

        return false;
    }

    private static string GetPath(IResource? parent)
    {
        string _path = "\\";
        if (parent != null)
        {
            _path = GetPath(parent.ParentResource) + parent.Title + "\\";
        }
        return _path;
    }
}
