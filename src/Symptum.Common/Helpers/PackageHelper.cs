using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using CsvHelper;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

/// <summary>
/// Provides helpers for managing Symptum packages: indexing, importing,
/// exporting and downloading package metadata and files.
///
/// The helper maintains a cache of package id -> metadata path mappings and
/// manages package storage folders used by the application.
/// </summary>
public class PackageHelper
{
    private class IdPath
    {
        /// <summary>
        /// Package identifier.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Relative path to the package metadata file inside the packages folder.
        /// </summary>
        public string? Path { get; set; }
    }

    /// <summary>
    /// File name used to store the package index CSV inside the packages folder.
    /// </summary>
    private static readonly string indexFileName = "PackageIndex" + CsvFileExtension;

    /// <summary>
    /// In-memory cache mapping package id -> metadata file name (relative to
    /// the PackagesFolder).
    /// </summary>
    private static readonly Dictionary<string, string> packageIdPathCache = [];

    /// <summary>
    /// StorageFile for the package index CSV.
    /// </summary>
    private static StorageFile? indexFile;

    private static bool _init = false;

    /// <summary>
    /// Folder where packages are stored/unpacked. Acts as the canonical
    /// packages directory for the application.
    /// </summary>
    public static StorageFolder? PackagesFolder { get; private set; }

    /// <summary>
    /// Temporary cache folder where downloaded .zip packages are stored before
    /// they are extracted and moved into the <see cref="PackagesFolder"/>.
    /// </summary>
    public static StorageFolder? PackageCacheFolder { get; private set; }

    private static StorageFolder? _exportFolder;

    /// <summary>
    /// Folder used for exporting packages (used by editor workflows).
    /// </summary>
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

    /// <summary>
    /// Prompts the user to select an export folder (when supported) or sets
    /// the provided <see cref="StorageFolder"/> as the export folder.
    /// </summary>
    /// <param name="folder">Optional folder to set as export folder. When
    /// <c>null</c> and folder picking is supported the user will be prompted.</param>
    /// <returns><c>true</c> if a different export folder was selected and set,
    /// otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Initializes package folders and loads the package index into the
    /// in-memory cache. This method is idempotent and will perform initialization only once.
    /// </summary>
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

    /// <summary>
    /// Writes the in-memory package index cache back to the index CSV file in the packages folder.
    /// </summary>
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

    /// <summary>
    /// Exports a package by saving its resources into an export folder and
    /// creating a zip archive. Returns <c>true</c> when the export succeeded.
    /// </summary>
    /// <param name="package">The package to export.</param>
    /// <returns><c>true</c> if export succeeded, otherwise <c>false</c>.</returns>
    public static async Task<bool> ExportPackageAsync(IPackageResource? package)
    {
        if (package != null && !string.IsNullOrWhiteSpace(package.Id) && await VerifyExportFolderAsync())
        {
            StorageFolder folder = await ExportFolder.CreateFolderAsync(package.Id, CreationCollisionOption.OpenIfExists); // Create a folder with the package's id as name
            await ResourceHelper.SaveResourceAsync(package, folder, exporting: true); // Save all the files to this new folder

            // NOTE: Should the resources be saved to an export folder first then archived from that folder?
            // (This is the current method, let's keep it like this for simplicity)
            // Or should the archive be created directly from the resources in the future?
            StorageFile zipFile = await ExportFolder.CreateFileAsync(package.Id + PackageFileExtension, CreationCollisionOption.ReplaceExisting);
            await StorageHelper.CreateZipFileFromFolderAsync(folder, zipFile);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Imports a package from a .zip file by extracting it into the packages
    /// folder and registering its metadata in the index cache.
    /// </summary>
    /// <param name="zipFile">Zip file to import.</param>
    /// <returns><c>true</c> when import succeeded, <c>false</c> when it failed,
    /// or <c>null</c> when the provided file was not a package.</returns>
    public static async Task<bool?> ImportPackageAsync(StorageFile? zipFile)
    {
        if (zipFile != null && zipFile.FileType.Equals(PackageFileExtension, StringComparison.InvariantCultureIgnoreCase)
            && PackageCacheFolder != null && PackagesFolder != null)
        {
            Stream zipStream;
//#if __WASM__
//            var buffer = await FileIO.ReadBufferAsync(zipFile); // NOTE: OpenStreamForReadAsync() crashes on WASM?
//            zipStream = new MemoryStream(buffer.ToArray());
//#else
            zipStream = await zipFile.OpenStreamForReadAsync();
//#endif

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

    /// <summary>
    /// Loads a package resource by id. First attempts to resolve a cached
    /// local metadata file; if not found it will attempt to download the
    /// package.
    /// </summary>
    /// <param name="packageId">Package identifier.</param>
    /// <returns>The loaded package resource or <c>null</c> if not available.</returns>
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

    /// <summary>
    /// Base URL used for hosted package pages.
    /// </summary>
    private static readonly string pagesUrl = "https://symptum.github.io/Symptum.Packages/"; // for now we'll use GitHub Pages to host the packages

    /// <summary>
    /// Raw GitHub URL to the packages repository used to fetch package files.
    /// </summary>
    private static readonly string repoUrl = "https://raw.githubusercontent.com/symptum/Symptum.Packages/main/";

    /// <summary>
    /// Name of the online package index file hosted under <see cref="pagesUrl"/>.
    /// </summary>
    private static readonly string onlinePackageIndex = "index.json";

    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// Attempts to download package metadata and assets for the given package
    /// id. Currently this method fetches an online package index and can be
    /// extended to download package archives from the configured repository.
    /// </summary>
    /// <param name="packageId">Package identifier to download.</param>
    /// <returns><c>true</c> if download and registration succeeded;
    /// otherwise <c>false</c>.</returns>
    public static async Task<bool> DownloadPackageAsync(string packageId)
    {
        string? json = null;
        if (NetworkInformation.GetInternetConnectionProfile() is ConnectionProfile connectionProfile
            && connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
        {
            HttpResponseMessage response = await httpClient.GetAsync(pagesUrl + onlinePackageIndex);
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
