using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

/// <summary>
/// Provides helpers for managing Symptum packages: indexing, importing,
/// exporting and downloading package metadata and files.
///
/// The helper maintains a cache of package id -> <see cref="PackageEntry"/>
/// mappings (the local package index) and manages package storage folders used
/// by the application. The local index records the installed version of each
/// package so that updates can be detected by comparing against the online
/// package index.
/// </summary>
public class PackageHelper
{
    private static readonly string indexFileName = "PackageIndex" + JsonFileExtension;
    private static readonly Dictionary<string, PackageEntry> packageCache = [];
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

            string json = await FileIO.ReadTextAsync(indexFile);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    List<PackageEntry>? entries = JsonSerializer.Deserialize<List<PackageEntry>>(json);
                    if (entries != null)
                    {
                        foreach (var entry in entries)
                        {
                            if (!string.IsNullOrWhiteSpace(entry.Id))
                                packageCache[entry.Id] = entry;
                        }
                    }
                }
                catch { }
            }
        }

        PackageManager.Initialize(LoadPackageAsync);

        _init = true;
    }

    private static readonly JsonSerializerOptions indexOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes the in-memory package index cache back to the index JSON file in the packages folder.
    /// </summary>
    public static async Task UpdatePackageCacheFile()
    {
        string json = JsonSerializer.Serialize(packageCache.Values.ToList(), indexOptions);

        if (indexFile != null)
            await FileIO.WriteTextAsync(indexFile, json);
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
                    PackageResource? package = ResourceManager.LoadPackageFromMetadata(json);
                    if (package != null && !string.IsNullOrWhiteSpace(package.Id))
                    {
                        packageCache[package.Id] = new PackageEntry
                        {
                            Id = package.Id,
                            Title = package.Title,
                            Version = package.Version,
                            Path = jsonFile.Name
                        };
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
    /// local metadata file; if not found it will attempt to download and
    /// import the package from the online index.
    /// </summary>
    /// <param name="packageId">Package identifier.</param>
    /// <returns>The loaded package resource or <c>null</c> if not available.</returns>
    public static async Task<IPackageResource?> LoadPackageAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId)) return null;

        if (await TryLoadCachedPackageAsync(packageId) is IPackageResource package)
            return package;

        if (await DownloadPackageAsync(packageId) && await TryLoadCachedPackageAsync(packageId) is IPackageResource downloadedPackage)
            return downloadedPackage;

        return null;
    }

    /// <summary>
    /// Loads an installed package from the local index cache when its metadata
    /// file is available in the <see cref="PackagesFolder"/>.
    /// </summary>
    private static async Task<IPackageResource?> TryLoadCachedPackageAsync(string packageId)
    {
        if (packageCache.TryGetValue(packageId, out PackageEntry? entry) &&
            !string.IsNullOrWhiteSpace(entry.Path) &&
            await PackagesFolder?.TryGetItemAsync(entry.Path) is StorageFile jsonFile &&
            jsonFile.FileType.Equals(JsonFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            return await ResourceHelper.LoadPackageResourceFromFileAsync(jsonFile);
        }

        return null;
    }

    #region Downloading

    private static readonly string baseUrl = "https://symptum.github.io/Symptum.Packages/"; // for now we'll use GitHub Pages to host the packages
    private static readonly string onlinePackageIndex = "index.json";
    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// Downloads a package by id from the online index. Fetches
    /// <c>index.json</c> from the packages URL, finds the entry matching
    /// <paramref name="packageId"/>, downloads the package archive referenced
    /// by that entry into the package cache folder and imports it.
    /// </summary>
    /// <param name="packageId">Package identifier to download.</param>
    /// <returns><c>true</c> if download and import succeeded;
    /// otherwise <c>false</c>.</returns>
    public static async Task<bool> DownloadPackageAsync(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId) || PackageCacheFolder == null) return false;

        if (NetworkInformation.GetInternetConnectionProfile() is not ConnectionProfile connectionProfile
            || connectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
            return false;

        try
        {
            string json = await httpClient.GetStringAsync(baseUrl + onlinePackageIndex);
            List<PackageEntry>? entries = JsonSerializer.Deserialize<List<PackageEntry>>(json);
            PackageEntry? entry = entries?.FirstOrDefault(e => e.Id == packageId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Path)) return false;

            string fileName = Path.GetFileName(entry.Path);
            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.EndsWith(PackageFileExtension, StringComparison.InvariantCultureIgnoreCase))
                fileName = packageId + PackageFileExtension;

            StorageFile zipFile = await PackageCacheFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            using (Stream zipStream = await zipFile.OpenStreamForWriteAsync())
            {
                using Stream responseStream = await httpClient.GetStreamAsync(baseUrl + entry.Path);
                await responseStream.CopyToAsync(zipStream);
            }

            return await ImportPackageAsync(zipFile) == true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Represents a package in the online package index or in the local package
/// cache. In the online index the <see cref="Path"/> is a relative URL to the
/// package archive that is combined with the base URL to get the download URL;
/// in the local cache it is the metadata file name used to load the installed
/// package.
/// </summary>
public class PackageEntry
{
    /// <summary>
    /// Unique identifier of the package (matches the package metadata id).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Title of the package.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Version of the package. In the online index this is the version
    /// available for download; in the local cache it is the installed version,
    /// which is compared against the index to detect updates.
    /// </summary>
    public Version? Version { get; set; }

    /// <summary>
    /// Location of the package. In the online index this is a relative path
    /// that is combined with the base URL to get the download URL. In the
    /// local cache this is the package metadata JSON file name relative to the
    /// packages folder.
    /// </summary>
    public string? Path { get; set; }
}
