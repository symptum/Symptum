using System.IO.Compression;
using Windows.Foundation;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

#if __WASM__
using static Uno.Storage.Pickers.FileSystemAccessApiInformation;
#endif

public class StorageHelper
{
    #region Properties

    private static bool isFileOpenPickerSupported = true;

    public static bool IsFileOpenPickerSupported { get => isFileOpenPickerSupported; }

    private static bool isFileSavePickerSupported = true;

    public static bool IsFileSavePickerSupported { get => isFileSavePickerSupported; }

    private static bool isFolderPickerSupported = true;

    public static bool IsFolderPickerSupported { get => isFolderPickerSupported; }

    #endregion

    public static void Initialize()
    {
#if __WASM__
        Uno.WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = Uno.WasmPickerConfiguration.FileSystemAccessApiWithFallback;
        isFileOpenPickerSupported = IsOpenPickerSupported;
        isFileSavePickerSupported = IsSavePickerSupported;
        isFolderPickerSupported = IsFolderPickerSupported;
#endif
    }

    #region Storage Methods

    private static async Task<StorageFolder?> SubFolderFuncAsync(StorageFolder? parent, string? path, Func<StorageFolder?, string, IAsyncOperation<StorageFolder>?> func)
    {
        if (path == null) return parent;

        StorageFolder? folder = parent;

        path = path.Trim(PathSeparator);
        var folders = path.Split(PathSeparator);

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrWhiteSpace(folderName))
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

    public static async Task<StorageFolder?> GetSubFolderAsync(StorageFolder? parent, string? path)
    {
        StorageFolder? folder = await SubFolderFuncAsync(parent, path,
            (f, name) => f?.GetFolderAsync(name));
        return folder;
    }

    public static async Task<StorageFolder?> CreateSubFoldersAsync(StorageFolder? parent, string? path = null)
    {
        StorageFolder? folder = await SubFolderFuncAsync(parent, path,
            (f, name) => f?.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists));
        return folder;
    }

    public static async Task<bool> WriteToFileAsync(StorageFile file, string content)
    {
        CachedFileManager.DeferUpdates(file);
        await FileIO.WriteTextAsync(file, content);
        await CachedFileManager.CompleteUpdatesAsync(file);
        return true;
    }

    #endregion

    #region Zip Archive

    public static async Task<bool> CreateZipFileFromFolderAsync(StorageFolder? sourceFolder, StorageFile? targetZipFile)
    {
        if (sourceFolder == null || targetZipFile == null) return false;

        using Stream zipToCreate = await targetZipFile.OpenStreamForWriteAsync();
        using ZipArchive archive = new(zipToCreate, ZipArchiveMode.Update);
        await UpdateArchiveAsync(archive, sourceFolder);

        return true;
    }

    private static async Task UpdateArchiveAsync(ZipArchive archive, StorageFolder sourceFolder, string? sourceFolderPath = null)
    {
        IReadOnlyList<StorageFile> files = await sourceFolder.GetFilesAsync();

        sourceFolderPath ??= sourceFolder.Path;
        foreach (StorageFile file in files)
        {
            string filePath
#if __WASM__
                = Path.Combine(sourceFolder.Path, file.Name); // file.Path returns the file's name and not it's actual path in WASM
#else
                = file.Path;
#endif
            string relPath = Path.GetRelativePath(sourceFolderPath, filePath);
            ZipArchiveEntry entry = archive.CreateEntry(relPath, CompressionLevel.SmallestSize);
            using Stream entryStream = entry.Open();
            Stream stream = await file.OpenStreamForReadAsync();
            await stream.CopyToAsync(entryStream);
        }

        IReadOnlyList<StorageFolder> subFolders = await sourceFolder.GetFoldersAsync();

        foreach (StorageFolder subFolder in subFolders)
        {
            await UpdateArchiveAsync(archive, subFolder, sourceFolderPath);
        }
    }

    #endregion
}
