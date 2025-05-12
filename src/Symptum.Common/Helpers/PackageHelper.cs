using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using CsvHelper;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

public class PackageHelper
{
    private class IdPath
    {
        public string? Id { get; set; }

        public string? Path { get; set; }
    }

    private static readonly string indexFileName = "PackageIndex" + CsvFileExtension;
    private static readonly Dictionary<string, string> packageIdPathCache = [];
    private static StorageFile? indexFile;
    private static bool _init = false;

    // This will be the work folder for ResourceHelper in the Symptum App.
    // This is not the case of Symptum.Editor as the data can be stored anywhere and have to be edited
    public static StorageFolder? PackagesFolder { get; private set; }

    // The "*.zip" packages will be downloaded here and they'll be extracted and moved to the PackagesFolder during import
    public static StorageFolder? PackageCacheFolder { get; private set; }

    private static StorageFolder? _exportFolder;

    // This folder will be used for exporting the packages in Symptum.Editor
    public static StorageFolder? ExportFolder
    {
        get => _exportFolder;
        private set
        {
            _exportFolder = value;
        }
    }

    #region Export Folder Handling

    [MemberNotNullWhen(true, nameof(ExportFolder), nameof(_exportFolder))]
    private static async Task<bool> VerifyExportFolderAsync()
    {
        bool pathExists = true;
        if (_exportFolder == null)
            pathExists = await SelectExportFolderAsync();
        return pathExists;
    }

    public static async Task<bool> SelectExportFolderAsync(StorageFolder? folder = null)
    {
        if (folder == null && StorageHelper.IsFolderPickerSupported)
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

#if WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WindowHelper.WindowHandle);
#endif
            folder = await folderPicker.PickSingleFolderAsync();
        }

        if (folder != null && _exportFolder != folder && _exportFolder?.Path != folder.Path)
        {
            ExportFolder = folder;
            return true;
        }

        return false;
    }

    #endregion

    public static async Task InitializeAsync()
    {
        if (_init) return;

        PackagesFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Packages", CreationCollisionOption.OpenIfExists);
        PackageCacheFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("Package Cache", CreationCollisionOption.OpenIfExists);

        if (PackagesFolder != null)
        {
            indexFile = await PackagesFolder.TryGetItemAsync(indexFileName) as StorageFile
                ?? await PackagesFolder.CreateFileAsync(indexFileName);

            string csv = await FileIO.ReadTextAsync(indexFile);
            if (!string.IsNullOrWhiteSpace(csv))
            {
                using StringReader stringReader = new(csv);
                using CsvReader reader = new(stringReader, CultureInfo.InvariantCulture);

                var records = reader.GetRecords<IdPath>();
                foreach (var record in records)
                {
                    packageIdPathCache.Add(record.Id, record.Path);
                }
            }
        }

        PackageManager.Initialize(LoadPackageAsync);

        _init = true;
    }

    public static async Task UpdatePackageCacheFile()
    {
        using StringWriter stringWriter = new();
        using CsvWriter csvWriter = new(stringWriter, CultureInfo.InvariantCulture);

        List<IdPath> list = [];

        foreach (var kvp in packageIdPathCache)
        {
            list.Add(new() { Id = kvp.Key, Path = kvp.Value });
        }

        csvWriter.WriteRecords(list);

        if (indexFile != null)
            await FileIO.WriteTextAsync(indexFile, stringWriter.ToString());
    }

    public static async Task<bool> ExportPackageAsync(IPackageResource? package)
    {
        if (package != null && !string.IsNullOrWhiteSpace(package.Id) && await VerifyExportFolderAsync())
        {
            StorageFolder folder = await ExportFolder.CreateFolderAsync(package.Id, CreationCollisionOption.OpenIfExists); // Create a folder with the package's id as name
            await ResourceHelper.SaveResourceAsync(package, folder); // Save all the files to this new folder

            // NOTE: Should the resources be saved to an export folder first then archived from that folder?
            // (This is the current method, let's keep it like this for simplicity)
            // Or should the archive be created directly from the resources in the future?
            StorageFile zipFile = await ExportFolder.CreateFileAsync(package.Id + PackageFileExtension, CreationCollisionOption.ReplaceExisting);
            await StorageHelper.CreateZipFileFromFolderAsync(folder, zipFile);
        }

        return false;
    }

    public static async Task<bool?> ImportPackageAsync(StorageFile? zipFile)
    {
        if (zipFile != null && zipFile.FileType.Equals(PackageFileExtension, StringComparison.InvariantCultureIgnoreCase)
            && PackageCacheFolder != null && PackagesFolder != null)
        {
            Stream zipStream;
#if __WASM__
            var buffer = await FileIO.ReadBufferAsync(zipFile); // NOTE: OpenStreamForReadAsync() crashes on WASM?
            zipStream = new MemoryStream(buffer.ToArray());
#else
            zipStream = await zipFile.OpenStreamForReadAsync();
#endif

            ZipArchive archive = new(zipStream, ZipArchiveMode.Read);

            string? jsonFileName = archive.Entries.FirstOrDefault(e =>
                Path.GetExtension(e.Name).Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))?.Name;

            archive.ExtractToDirectory(PackagesFolder.Path, true);

            if (jsonFileName != null && await PackagesFolder?.TryGetItemAsync(jsonFileName) is StorageFile jsonFile &&
                jsonFile.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    string json = await FileIO.ReadTextAsync(jsonFile);
                    var package = ResourceManager.LoadPackageFromMetadata(json);
                    if (package != null)
                    {
                        packageIdPathCache.Add(package.Id, jsonFile.Name);
                        await UpdatePackageCacheFile();
                        return true;
                    }
                }
                catch { }
            }
        }
        return false;
    }

    public static async Task<IPackageResource?> LoadPackageAsync(string packageId)
    {
        if (!string.IsNullOrWhiteSpace(packageId))
        {
            if (packageIdPathCache.TryGetValue(packageId, out string? path) && await PackagesFolder?.TryGetItemAsync(path) is StorageFile jsonFile &&
                jsonFile.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                return await ResourceHelper.LoadPackageResourceFromFileAsync(jsonFile);
            }
            else
            {
                await DownloadPackageAsync(packageId);
            }
        }

        return null;
    }

    #region Downloading

    private static readonly string pagesUrl = "https://symptum.github.io/Symptum.Packages/"; // for now we'll use GitHub Pages to host the packages
    private static readonly string repoUrl = "https://raw.githubusercontent.com/symptum/Symptum.Packages/main/";
    private static readonly string onlinePackageIndex = "test.json";

    public static async Task<bool> DownloadPackageAsync(string packageId)
    {
        string? json = null;
        if (NetworkInformation.GetInternetConnectionProfile() is ConnectionProfile connectionProfile
            && connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(pagesUrl + onlinePackageIndex);
            if (response?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                json = await response.Content.ReadAsStringAsync();
                var obj = JsonSerializer.Deserialize<PackageResource>(json);
            }
        }

        return false;
    }

    #endregion
}
