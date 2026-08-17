using Symptum.Core.Extensions;
using Symptum.Core.Helpers;
using Symptum.Core.Management.Resources;
using Symptum.Markdown;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.Helpers;

/// <summary>
/// Provides helper methods for selecting a working folder and loading, saving,
/// copying and removing resource files used by the application.
///
/// This class centralizes filesystem interactions for resources (images, audio,
/// markdown, CSV and package metadata) and maintains an in-memory mapping
/// between FileResource instances and their corresponding <see cref="StorageFile"/>.
/// </summary>
/// <remarks>
/// Loading resources is done by both Symptum App and Editor. But the main application does not need
/// loading all the children recursively. But the editor needs to load all the children recursively.
/// So <see cref="RecursivelyLoadChildren"/> is set in the editor to true.
/// Editor also has to save and delete resources. So those are not to be used in the main application.
/// The main application only needs to load resources one level at a time and use them.
/// </remarks>
public class ResourceHelper
{
    private static StorageFolder? _workFolder;

    /// <summary>
    /// Gets or sets a value indicating whether child resources
    /// should be loaded recursively when loading a parent resource.
    /// When <c>true</c>, the <see cref="LoadResourceAsync"/>
    /// method will load all children of a resource after loading the parent.
    /// </summary>
    public static bool RecursivelyLoadChildren { get; set; } = false;

    /// <summary>
    /// Gets or sets the current working folder used for resource operations.
    /// Setting this property will raise the <see cref="WorkFolderChanged"/> event.
    /// When <c>null</c>, no work folder is selected.
    /// </summary>
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

    /// <summary>
    /// Closes the currently selected work folder and clears loaded resources.
    /// </summary>
    public static void CloseWorkFolder()
    {
        WorkFolder = null;
        ResourceManager.Resources.Clear();
    }

    /// <summary>
    /// Prompts the user to select a work folder (when supported) or accepts a
    /// provided <see cref="StorageFolder"/> and sets it as the current
    /// work folder.
    /// </summary>
    /// <param name="folder">Optional folder to set as the work folder. If
    /// <c>null</c> and folder picking is supported, a folder picker will be
    /// shown to the user.</param>
    /// <returns><c>true</c> if a different work folder was selected and set;
    /// otherwise <c>false</c>.</returns>
    public static async Task<bool> SelectWorkFolderAsync(StorageFolder? folder = null)
    {
        if (folder == null /*&& StorageHelper.IsFolderPickerSupported*/)
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

#if WINDOWS && !HAS_UNO
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

    /// <summary>
    /// Returns the files inside the currently selected work folder, or
    /// <c>null</c> when no work folder is set.
    /// </summary>
    /// <returns>A read-only list of <see cref="StorageFile"/> instances or
    /// <c>null</c> if no work folder is available.</returns>
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

    /// <summary>
    /// Maps in-memory <see cref="FileResource"/> instances to their
    /// corresponding <see cref="StorageFile"/> so file-backed resources can
    /// be loaded, saved and copied without repeatedly resolving paths.
    /// </summary>
    private static readonly Dictionary<FileResource, StorageFile> fileMap = [];

    /// <summary>
    /// Opens the underlying <see cref="StorageFile"/> for a given
    /// <see cref="FileResource"/> and returns a read-only random access stream.
    /// </summary>
    /// <param name="fileResource">The file resource to open. The resource must
    /// have a non-empty <see cref="FileResource.FilePath"/>.</param>
    /// <returns>An <see cref="IRandomAccessStream"/> for reading the file, or
    /// <c>null</c> if the file cannot be opened.</returns>
    public static async Task<IRandomAccessStream?> OpenFileForReadAsync(FileResource fileResource)
    {
        if (string.IsNullOrEmpty(fileResource.FilePath))
            return null;

        if (fileMap.TryGetValue(fileResource, out StorageFile? file))
            return await file.OpenReadAsync();

        try
        {
            file = await StorageFile.GetFileFromPathAsync(fileResource.FilePath);
            fileMap.TryAdd(fileResource, file);
            return await file.OpenReadAsync();
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Loading Resources

    /// <summary>
    /// Loads resources from the current work folder. This enumerates files in
    /// the work folder and attempts to create resource objects for recognized
    /// file types (CSV, Markdown, JSON packages, images, audio, etc.).
    /// </summary>
    public static async Task LoadResourcesFromWorkPathAsync()
    {
        var files = await GetFilesFromWorkPathAsync();
        await LoadResourcesFromFilesAsync(files);
    }

    /// <summary>
    /// Loads resources from a collection of <see cref="StorageFile"/> objects.
    /// Each file will be inspected and the appropriate resource type will be
    /// created and added to the resource manager or to the provided parent.
    /// </summary>
    /// <param name="files">The files to load resources from.</param>
    /// <param name="parent">Optional parent resource to attach created
    /// resources to.</param>
    /// <param name="sourceFolder">Optional folder that acts as the source of
    /// the files.</param>
    public static async Task LoadResourcesFromFilesAsync(IEnumerable<StorageFile>? files, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            await LoadResourceFromFileAsync(file, parent, sourceFolder);
        }
    }

    /// <summary>
    /// Loads a single resource from a file. The file type is used to determine
    /// the resource implementation to create (CSV, Markdown, JSON package,
    /// image, audio, ...).
    /// </summary>
    /// <param name="file">The file to load the resource from.</param>
    /// <param name="parent">Optional parent resource to attach the created
    /// resource to.</param>
    /// <param name="sourceFolder">Optional source folder for resolving
    /// relative paths.</param>
    /// <returns>The created <see cref="IResource"/> or <c>null</c> if the file
    /// type is not recognized or loading failed.</returns>
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

    /// <summary>
    /// Creates a <see cref="CsvFileResource"/> from a CSV <see cref="StorageFile"/>
    /// if the file's contents match a known CSV resource type. The created
    /// resource is added to the resource manager or attached to the provided
    /// parent when appropriate.
    /// </summary>
    /// <param name="file">The CSV file to read.</param>
    /// <param name="parent">Optional parent resource to attach the created
    /// CSV resource to.</param>
    /// <returns>The created <see cref="CsvFileResource"/> or <c>null</c> if the
    /// file could not be parsed into a CSV resource.</returns>
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

    /// <summary>
    /// Creates a <see cref="MarkdownFileResource"/> from a markdown
    /// <see cref="StorageFile"/>, registers it with the resource manager and
    /// resolves any dependencies defined in the markdown content.
    /// </summary>
    /// <param name="file">The markdown file to read.</param>
    /// <param name="parent">Optional parent resource to attach the created
    /// markdown resource to.</param>
    /// <returns>The created <see cref="MarkdownFileResource"/> or <c>null</c>
    /// if the file is <c>null</c>.</returns>
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

            ResourceManager.ResolveDependencies(markdownFileResource);

            return markdownFileResource;
        }

        return null;
    }

    /// <summary>
    /// Creates an <see cref="ImageFileResource"/> for an image file and
    /// registers the underlying <see cref="StorageFile"/> for later use.
    /// </summary>
    /// <param name="file">The image file to load.</param>
    /// <param name="parent">Optional parent resource to attach the created
    /// image resource to.</param>
    /// <returns>The created image resource or <c>null</c> when <paramref name="file"/>
    /// is <c>null</c>.</returns>
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

    /// <summary>
    /// Creates an <see cref="AudioFileResource"/> for an audio file and
    /// registers the underlying <see cref="StorageFile"/> for later use.
    /// </summary>
    /// <param name="file">The audio file to load.</param>
    /// <param name="parent">Optional parent resource to attach the created
    /// audio resource to.</param>
    /// <returns>The created audio resource or <c>null</c> when
    /// <paramref name="file"/> is <c>null</c>.</returns>
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

    /// <summary>
    /// Loads a package resource from its metadata JSON file, registers the
    /// package and recursively loads its children.
    /// </summary>
    /// <param name="file">The package metadata file.</param>
    /// <param name="parent">Optional parent to attach the package to.</param>
    /// <param name="sourceFolder">Optional source folder used to resolve
    /// referenced files.</param>
    /// <returns>The loaded <see cref="PackageResource"/> or <c>null</c> if
    /// loading failed.</returns>
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

    /// <summary>
    /// Loads a resource into memory. The method dispatches to specialized
    /// loaders depending on the resource type (text, media, metadata,...),
    /// initializes the resource and loads its children when applicable.
    /// </summary>
    /// <param name="resource">The resource to load.</param>
    /// <param name="parent">Optional parent to initialize against.</param>
    /// <param name="sourceFolder">Optional folder used to resolve file
    /// references.</param>
    public static async Task LoadResourceAsync(IResource? resource, IResource? parent = null, StorageFolder? sourceFolder = null)
    {
        if (resource == null) return;
        if (resource.HasInitialized)
        {
            // This to avoid loading the resource multiple times. 
            // But sometimes the resource may have been loaded without a parent.
            // We will set it here.
            resource.InitializeResource(parent);
            return;
        }

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

            if (RecursivelyLoadChildren) await LoadChildrenAsync(resource, sourceFolder);
        }
        ResourceManager.ResolveDependencies(resource);
    }

    /// <summary>
    /// Loads the child resources of a resource by calling
    /// <see cref="LoadResourceAsync(IResource?, IResource?, StorageFolder?)"/> for
    /// each child.
    /// </summary>
    /// <param name="resource">The parent resource whose children will be
    /// loaded.</param>
    /// <param name="sourceFolder">Optional folder used to resolve child
    /// file references.</param>
    public static async Task LoadChildrenAsync(IResource? resource, StorageFolder? sourceFolder = null)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            foreach (var child in resource.ChildrenResources)
            {
                await LoadResourceAsync(child, resource, sourceFolder);
            }
        }
    }

    /// <summary>
    /// Resolves a relative resource file path into a <see cref="StorageFile"/>
    /// using the provided source folder or the currently selected work
    /// folder.
    /// </summary>
    /// <param name="path">Relative path to the resource file.</param>
    /// <param name="sourceFolder">Optional source folder for resolution.</param>
    /// <returns>The resolved <see cref="StorageFile"/> or <c>null</c> when
    /// the file cannot be found.</returns>
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

    /// <summary>
    /// Loads the text contents for a <see cref="TextFileResource"/> (such as
    /// CSV or Markdown) from storage and populates the resource.
    /// </summary>
    /// <param name="textResource">The text resource to load.</param>
    /// <param name="sourceFolder">Optional folder to resolve the file from.</param>
    private static async Task LoadTextFileResourceAsync(TextFileResource textResource, StorageFolder? sourceFolder = null)
    {
        if (await GetResourceFileAsync(textResource.FilePath, sourceFolder) is StorageFile textFile)
        {
            string text = await FileIO.ReadTextAsync(textFile);
            ResourceManager.LoadResourceFileText(textResource, text);
        }
    }

    /// <summary>
    /// Locates the media file for a <see cref="MediaFileResource"/>, sets
    /// its extension and registers the underlying <see cref="StorageFile"/>
    /// in the internal file map.
    /// </summary>
    /// <param name="mediaResource">The media resource to load.</param>
    /// <param name="sourceFolder">Optional folder to resolve the media file.</param>
    private static async Task LoadMediaFileResourceAsync(MediaFileResource mediaResource, StorageFolder? sourceFolder = null)
    {
        if (await GetResourceFileAsync(mediaResource.FilePath, sourceFolder) is StorageFile mediaFile)
        {
            mediaResource.SetMediaFileExtension(mediaFile.FileType.ToLower());
            fileMap.TryAdd(mediaResource, mediaFile);
        }
    }

    /// <summary>
    /// Loads separate metadata for a <see cref="MetadataResource"/> from its
    /// JSON metadata file when the resource uses split metadata.
    /// </summary>
    /// <param name="resource">The metadata resource to populate.</param>
    /// <param name="sourceFolder">Optional folder to resolve the metadata
    /// file from.</param>
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

    /// <summary>
    /// Saves a resource to storage. The method delegates to specialized
    /// save operations depending on the resource type (text files, media
    /// files or metadata/package resources). Child resources can be saved
    /// recursively when <paramref name="saveChildren"/> is <c>true</c>.
    /// </summary>
    /// <param name="resource">The resource to save.</param>
    /// <param name="targetFolder">Optional target folder to save into. When
    /// <c>null</c> the current work folder is used when available.</param>
    /// <param name="saveChildren">If <c>true</c> children will be saved
    /// recursively.</param>
    /// <param name="exporting">When <c>true</c> perform any exporting-time
    /// transformations (for example markdown optimization).</param>
    /// <returns><c>true</c> when the save was successful, otherwise
    /// <c>false</c>.</returns>
    public static async Task<bool> SaveResourceAsync(IResource? resource, StorageFolder? targetFolder = null, bool saveChildren = true, bool exporting = false)
    {
        if (resource == null) return false;

        if (resource is TextFileResource textResource)
        {
            // CSVs and Markdowns
            return await SaveTextFileResourceAsync(textResource, targetFolder, exporting);
        }
        else if (resource is MediaFileResource mediaResource)
        {
            // Images and Audios
            return await CopySaveFileResourceAsync(mediaResource, targetFolder);
        }
        else
        {
            bool result = !saveChildren || await SaveChildrenAsync(resource, targetFolder, exporting);

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

    /// <summary>
    /// Writes the contents of a <see cref="TextFileResource"/> (CSV or
    /// Markdown) to storage, optionally performing exporting transformations.
    /// </summary>
    /// <param name="textResource">The text resource to save.</param>
    /// <param name="targetFolder">Optional target folder to save into.</param>
    /// <param name="exporting">When <c>true</c> apply export-specific
    /// transformations.</param>
    /// <returns><c>true</c> when the file was written successfully.</returns>
    private static async Task<bool> SaveTextFileResourceAsync(TextFileResource textResource, StorageFolder? targetFolder = null, bool exporting = false)
    {
        if (textResource == null) return false;

        string subFolderPath = ResourceManager.GetRelativeResourceFolderPath(textResource);
        string? fileName = ResourceManager.GetResourceFileName(textResource);
        textResource.FilePath = subFolderPath + fileName + textResource.FileExtension;
        StorageFile? saveFile = await PickSaveFileAsync(fileName, textResource.FileExtension, $"{textResource.FileType} File", targetFolder, subFolderPath);

        if (saveFile != null)
        {
            string? text = ResourceManager.WriteResourceFileText(textResource);
            if (exporting && textResource is MarkdownFileResource markdownResource && NeedsOptimization(textResource))
            {
                text = MarkdownManager.GetOptimizedMarkdown(markdownResource);
            }
            if (text != null)
                return await StorageHelper.WriteToFileAsync(saveFile, text);
        }

        return false;
    }

    /// <summary>
    /// Copies a file-backed resource (image or audio) into the target
    /// storage location and updates the resource's file path. The method
    /// uses the internal file map when available to access the source
    /// <see cref="StorageFile"/>.
    /// </summary>
    /// <param name="fileResource">The file resource to copy.</param>
    /// <param name="targetFolder">Optional target folder to copy into.</param>
    /// <returns><c>true</c> when the copy completed or was not required.</returns>
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

    /// <summary>
    /// Saves a resource's metadata as JSON into storage. Returns <c>true</c>
    /// when the metadata file was written successfully.
    /// </summary>
    /// <typeparam name="T">Type of the metadata resource.</typeparam>
    /// <param name="resource">The metadata resource to save.</param>
    /// <param name="targetFolder">Optional target folder to save into.</param>
    /// <returns><c>true</c> when the metadata file was written.</returns>
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

    /// <summary>
    /// Saves all child resources of a resource recursively.
    /// </summary>
    /// <param name="resource">Parent resource whose children will be saved.</param>
    /// <param name="targetFolder">Optional target folder to save into.</param>
    /// <param name="exporting">Propagates the exporting flag to children.</param>
    /// <returns><c>true</c> when all children were saved successfully.</returns>
    private static async Task<bool> SaveChildrenAsync(IResource? resource, StorageFolder? targetFolder = null, bool exporting = false)
    {
        if (resource != null && resource.CanHandleChildren && resource.ChildrenResources != null)
        {
            bool allChildrenSaved = true;
            foreach (var child in resource.ChildrenResources)
            {
                allChildrenSaved &= await SaveResourceAsync(child, targetFolder, exporting: exporting);
            }
            return allChildrenSaved;
        }
        return true;
    }

    /// <summary>
    /// Picks or creates a file to save into. When a work folder is available
    /// the file will be created inside it (optionally under subfolders),
    /// otherwise a platform file save picker will be shown.
    /// </summary>
    /// <param name="name">Suggested file name without extension.</param>
    /// <param name="extension">File extension (including dot).</param>
    /// <param name="fileType">User-friendly file type name for pickers.</param>
    /// <param name="targetFolder">Optional target folder to create the file in.</param>
    /// <param name="subFolder">Optional relative subfolder path under the
    /// target folder.</param>
    /// <returns>The picked or created <see cref="StorageFile"/>, or
    /// <c>null</c> when the operation was canceled or invalid parameters were provided.</returns>
    internal static async Task<StorageFile?> PickSaveFileAsync(string? name, string? extension, string? fileType, StorageFolder? targetFolder = null, string? subFolder = null)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(extension) ||
            string.IsNullOrEmpty(fileType)) return null;

        StorageFile? saveFile = null;
        targetFolder ??= _workFolder;
        if (targetFolder != null /*&& StorageHelper.IsFolderPickerSupported*/)
        {
            var folder = await StorageHelper.GetSubFolderAsync(targetFolder, subFolder, true);
            if (folder != null) saveFile = await folder.CreateFileAsync(name + extension, CreationCollisionOption.ReplaceExisting);
        }
        else
        {
            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = name
            };
            fileSavePicker.FileTypeChoices.Add(fileType, [extension]);

#if WINDOWS && !HAS_UNO
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, WindowHelper.WindowHandle);
#endif
            saveFile = await fileSavePicker.PickSaveFileAsync();

#if HAS_UNO && DESKTOP
            if (saveFile != null)
            {
                // Workaround for FileSavePicker.PickSaveFileAsync() in Linux which returns a StorageFile that doesn't exist yet,
                // which causes an exception when trying to open it for writing.
                saveFile = await StorageHelper.EnsureStorageFileExistsAsync(saveFile);
            }
#endif
        }
        return saveFile;
    }

    /// <summary>
    /// Copies a <see cref="StorageFile"/> into the specified target folder
    /// or uses the save picker to create a copy when a work folder is not
    /// available.
    /// </summary>
    /// <param name="file">Source file to copy.</param>
    /// <param name="name">Target file name without extension.</param>
    /// <param name="extension">Target file extension (including dot).</param>
    /// <param name="fileType">User-friendly file type name for pickers.</param>
    /// <param name="targetFolder">Optional target folder to copy into.</param>
    /// <param name="subFolder">Optional relative subfolder path under the
    /// target folder.</param>
    /// <returns>The saved <see cref="StorageFile"/> or <c>null</c> when the
    /// operation failed.</returns>
    private static async Task<StorageFile?> CopyFileAsync(StorageFile? file, string? name, string? extension, string? fileType, StorageFolder? targetFolder = null, string? subFolder = null)
    {
        StorageFile? saveFile = file;
        targetFolder ??= _workFolder;

        if (file == null || string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(extension) ||
            string.IsNullOrEmpty(fileType)) return saveFile;

        if (targetFolder != null /*&& StorageHelper.IsFolderPickerSupported*/)
        {
            var folder = await StorageHelper.GetSubFolderAsync(targetFolder, subFolder, true);
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

    /// <summary>
    /// Removes a resource from the resource manager. If <paramref name="delete"/>
    /// is <c>true</c> and the resource represents a file or metadata, the
    /// corresponding files will be deleted from disk when the work folder is
    /// available.
    /// </summary>
    /// <param name="resource">The resource to remove.</param>
    /// <param name="delete">If set to <c>true</c>, attempt to delete
    /// associated files from storage.</param>
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

    /// <summary>
    /// Deletes the physical file associated with a <see cref="FileResource"/>
    /// from the current work folder when folder-based storage is available.
    /// </summary>
    /// <param name="fileResource">The file resource whose file will be deleted.</param>
    private static async Task DeleteResourceFileAsync(FileResource? fileResource)
    {
        if (fileResource == null) return;
        if (_workFolder != null /*&& StorageHelper.IsFolderPickerSupported*/)
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

    /// <summary>
    /// Deletes metadata JSON and the resource folder for a metadata resource
    /// when the current work folder is available.
    /// </summary>
    /// <param name="resource">The metadata resource to delete on disk.</param>
    private static async Task DeleteMetadataAsync(MetadataResource? resource)
    {
        if (resource == null) return;
        if (_workFolder != null /*&& StorageHelper.IsFolderPickerSupported*/)
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

    /// <summary>
    /// Removes (and optionally deletes) all child resources of the given
    /// resource by invoking <see cref="RemoveResourceAsync(IResource?, bool)"/>
    /// for each child.
    /// </summary>
    /// <param name="resource">The parent resource whose children will be removed.</param>
    /// <param name="delete">If <c>true</c>, child resource files and metadata
    /// will also be deleted from disk.</param>
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

    #region Resource Optimization

    /// <summary>
    /// Set of resource IDs that should have export-time optimization applied.
    /// </summary>
    /// <example>Markdown optimization during export.</example>
    public static readonly HashSet<string> ResourcesToOptimize = [];

    /// <summary>
    /// Returns <c>true</c> when the given resource's ID is included in the
    /// optimization set and therefore requires export-time optimization.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns><c>true</c> when optimization is needed.</returns>
    private static bool NeedsOptimization(IResource resource) => resource != null && !string.IsNullOrEmpty(resource.Id) && ResourcesToOptimize.Contains(resource.Id);

    #endregion

    /// <summary>
    /// Raised when the current <see cref="WorkFolder"/> is changed. The
    /// event handler receives the new <see cref="StorageFolder"/> or
    /// <c>null</c> when the work folder is closed.
    /// </summary>
    public static event EventHandler<StorageFolder?> WorkFolderChanged;
}
