using System.IO.Compression;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

/// <summary>
/// Utility helpers for working with StorageFile and StorageFolder instances.
///
/// Contains helpers to resolve/create nested folders, write text files with
/// cached updates, ensure storage files exist, and create zip archives from
/// folders on disk. The class also exposes flags that indicate whether the
/// platform file/folder pickers are supported.
/// </summary>
public class StorageHelper
{
    #region Storage Methods

    /// <summary>
    /// Gets the nested folder structure defined by <paramref name="path"/>
    /// under the <paramref name="parent"/> and returns the final
    /// folder. If <paramref name="createIfMissing"/> is <c>true</c>, intermediate
    /// folders will be created when missing.
    /// </summary>
    /// <param name="parent">Starting folder for resolution. When <c>null</c>
    /// the path is considered absolute.</param>
    /// <param name="path">Relative path consisting of folder segments.
    /// </param>
    /// <param name="createIfMissing">When <c>true</c>, missing folders will be created.</param>
    /// <returns>The resolved <see cref="StorageFolder"/> or <c>null</c> when
    /// the folder does not exist or cannot be created.</returns>
    public static async Task<StorageFolder?> GetSubFolderAsync(StorageFolder? parent, string? path, bool createIfMissing = false)
    {
        if (parent == null) return null;
        if (path == null || string.Equals(path, PathSeparator)) return parent;

        StorageFolder folder = parent;

        path = path.Trim(PathSeparator);
        var folders = path.Split(PathSeparator);

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                try
                {
                    // During resource loading: GetFolderAsync works fine under Android SAF
                    // given that we have access to the parent folder, we can get sub folders.
                    // But when saving resources it fails while trying to get an existing folder
                    // using CreateFolderAsync with OpenIfExists option.
                    // It only fails when there is an existing folder. It can create a new one without issues.
                    // So we will use a common approach to get the folder, if it doesn't exist we will create it.
                    if (await folder.TryGetItemAsync(folderName) is StorageFolder f)
                        folder = f;
                    else if (createIfMissing)
                        folder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);
                }
                catch { return null; }
            }
        }

        return folder;
    }

    /// <summary>
    /// Writes text content to a <see cref="StorageFile"/> using the
    /// <see cref="CachedFileManager"/> API to defer and complete updates.
    /// This ensures the file is written correctly on platforms that require deferred updates.
    /// </summary>
    /// <param name="file">Destination storage file.</param>
    /// <param name="content">Text content to write.</param>
    /// <returns><c>true</c> when the write completed successfully.</returns>
    public static async Task<bool> WriteToFileAsync(StorageFile file, string content)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            CachedFileManager.DeferUpdates(file);
            try
            {
                await FileIO.WriteTextAsync(file, content);
            }
            finally
            {
                await CachedFileManager.CompleteUpdatesAsync(file);
            }

            return true;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Ensures that the provided <see cref="StorageFile"/> exists on disk.
    /// When the file has a parent folder this method will (re)create the
    /// file using ReplaceExisting to guarantee it can be opened for writing.
    /// </summary>
    /// <param name="file">The storage file to ensure exists.</param>
    /// <returns>The existing or recreated <see cref="StorageFile"/>.</returns>
    public static async Task<StorageFile> EnsureStorageFileExistsAsync(StorageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        try
        {
            if (!string.IsNullOrWhiteSpace(file.Path) && File.Exists(file.Path))
                return file;
        }
        catch { }

        StorageFolder? parent = await file.GetParentAsync();
        if (parent != null)
        {
            return await parent.CreateFileAsync(file.Name, CreationCollisionOption.ReplaceExisting);
        }

        return file;
    }

    #endregion

    #region Zip Archive

    /// <summary>
    /// Creates or updates a zip archive file containing the contents of the
    /// <paramref name="sourceFolder"/>. Returns <c>true</c> when the
    /// archive was created successfully.
    /// </summary>
    /// <param name="sourceFolder">Folder to archive.</param>
    /// <param name="targetZipFile">Target zip file to create or update.</param>
    /// <returns><c>true</c> on success; <c>false</c> when parameters are invalid.</returns>
    public static async Task<bool> CreateZipFileFromFolderAsync(StorageFolder? sourceFolder, StorageFile? targetZipFile)
    {
        if (sourceFolder == null || targetZipFile == null) return false;

        using Stream zipToCreate = await targetZipFile.OpenStreamForWriteAsync();
        using ZipArchive archive = new(zipToCreate, ZipArchiveMode.Update);
        await UpdateArchiveAsync(archive, sourceFolder);

        return true;
    }

    /// <summary>
    /// Recursively adds files and subfolders from <paramref name="sourceFolder"/>
    /// into the provided zip <paramref name="archive"/>. The resulting
    /// entry paths are relative to <paramref name="sourceFolderPath"/>
    /// when supplied (defaults to the root folder path).
    /// </summary>
    /// <param name="archive">Destination ZipArchive to populate.</param>
    /// <param name="sourceFolder">Folder to read files from.</param>
    /// <param name="sourceFolderPath">Optional root path used to compute
    /// relative paths inside the archive.</param>
    private static async Task UpdateArchiveAsync(ZipArchive archive, StorageFolder sourceFolder, string? sourceFolderPath = null)
    {
        IReadOnlyList<StorageFile> files = await sourceFolder.GetFilesAsync();

        sourceFolderPath ??= sourceFolder.Path;
        foreach (StorageFile file in files)
        {
            string filePath
#if __WASM__
                = Path.Combine(sourceFolder.Path, file.Name); // NOTE: file.Path returns the file's name and not it's actual path in WASM
#else
                = file.Path;
#endif
            string relPath = Path.GetRelativePath(sourceFolderPath, filePath);
            ZipArchiveEntry entry = archive.CreateEntry(relPath, CompressionLevel.SmallestSize);
            using Stream entryStream = entry.Open();
            using Stream stream = await file.OpenStreamForReadAsync();
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
