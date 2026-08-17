using Symptum.Common.Helpers;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Resources;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.ProjectSystem;

/// <summary>
/// Manages a simple project system on top of the resource manager. The
/// ProjectSystemManager can open a work folder, discover a project file and
/// selectively load or save resources according to project entries.
/// </summary>
public class ProjectSystemManager
{
    /// <summary>
    /// When <c>true</c> the project manager will track resources in a
    /// project file and save entries there. When <c>false</c> resources are
    /// handled directly by the resource helper.
    /// </summary>
    public static bool UseProjectManager { get; set; } = false;

    /// <summary>
    /// Currently loaded project or <c>null</c> when no project is active.
    /// Setting this property raises the <see cref="CurrentProjectChanged"/>
    /// event when the value changes.
    /// </summary>
    public static Project? CurrentProject
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                CurrentProjectChanged?.Invoke(null, value);
            }
        }
    }

    /// <summary>
    /// Prompts the user to select (or uses the provided) work folder and
    /// processes files from that folder. If a folder is selected the method
    /// clears the current resource state and attempts to discover and load
    /// project files contained in the folder.
    /// </summary>
    /// <param name="folder">Optional folder to open as the work folder.</param>
    /// <returns><c>true</c> when a new work folder was selected and processed.</returns>
    public static async Task<bool> OpenWorkFolderAsync(StorageFolder? folder = null)
    {
        bool result = await ResourceHelper.SelectWorkFolderAsync(folder);
        if (result /*&& StorageHelper.IsFolderPickerSupported*/)
        {
            CurrentProject = null;
            UseProjectManager = false;
            ResourceManager.Resources.Clear();
            await ProcessFilesFromWorkPathAsync();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Enumerates files in the current work folder and processes each file
    /// (either as a resource or as a project file).
    /// </summary>
    private static async Task ProcessFilesFromWorkPathAsync()
    {
        var files = await ResourceHelper.GetFilesFromWorkPathAsync();
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            await ProcessFileAsync(file);
        }
    }

    /// <summary>
    /// Processes a single file from the work folder. Project files are
    /// deserialized and loaded, other recognized resource files are passed
    /// to <see cref="ResourceHelper"/> for loading.
    /// </summary>
    /// <param name="file">File to process.</param>
    private static async Task ProcessFileAsync(StorageFile file)
    {
        if (file == null) return;

        if (file.FileType.Equals(ProjectFileExtension, StringComparison.InvariantCultureIgnoreCase))
        {
            await LoadProjectFromFileAsync(file);
        }
        else
            await ResourceHelper.LoadResourceFromFileAsync(file);
    }

    /// <summary>
    /// Creates (or returns an existing) hierarchy of <see cref="ProjectFolder"/>
    /// resources matching the provided relative <paramref name="path"/>.
    /// The returned <see cref="ProjectFolder"/> corresponds to the final
    /// segment of the path.
    /// </summary>
    /// <param name="path">Relative folder path (using the configured
    /// path separator).</param>
    /// <returns>The final project folder resource or <c>null</c> when the
    /// provided path is empty.</returns>
    private static ProjectFolder? CreateOrGetProjectFolderResources(string? path)
    {
        ProjectFolder? parent = null;
        if (string.IsNullOrEmpty(path)) return null;
        var folders = path.Split(PathSeparator);

        for (int i = 0; i < folders.Length; i++)
        {
            string folderName = folders[i];
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                var resources = parent?.ChildrenResources ?? ResourceManager.Resources;
                if (resources.FirstOrDefault(x =>
                    x is ProjectFolder f && folderName.Equals(f.Title, StringComparison.InvariantCultureIgnoreCase))
                    is not ProjectFolder folder)
                {
                    folder = new() { Title = folderName };
                    ((IResource)folder).InitializeResource(parent);

                    if (parent != null)
                        parent.AddChildResource(folder);
                    else
                        ResourceManager.Resources.Add(folder);
                }
                parent = folder;
            }
        }

        return parent;
    }

    /// <summary>
    /// Loads a project definition from a project file and imports the
    /// listed entries into the resource manager. Entries with empty paths
    /// are ignored as those resources are considered part of the folder root.
    /// </summary>
    /// <param name="file">Project file to load.</param>
    private static async Task LoadProjectFromFileAsync(StorageFile? file)
    {
        if (file == null) return;

        string xml = await FileIO.ReadTextAsync(file);
        Project? project = Project.Deserialize(xml);
        if (project != null && project.Entries != null)
        {
            project.Name = file.DisplayName;
            CurrentProject = project;
            UseProjectManager = true;
            foreach (ProjectEntry entry in CurrentProject.Entries)
            {
                // NOTE: To prevent reloading the resources in the same folder as the project file.
                // They will be loaded regardless.
                if (string.IsNullOrEmpty(entry.Path) || entry.Path == PathSeparator.ToString())
                    continue;

                StorageFolder? sourceFolder = await StorageHelper.GetSubFolderAsync(ResourceHelper.WorkFolder, entry.Path);
                if (sourceFolder != null)
                {
                    StorageFile? sourceFile = await sourceFolder.TryGetItemAsync(entry.Name) as StorageFile;
                    IResource? parent = CreateOrGetProjectFolderResources(entry.Path);
                    await ResourceHelper.LoadResourceFromFileAsync(sourceFile, parent, sourceFolder);
                }
            }
        }
    }

    /// <summary>
    /// Saves all top-level resources and, when the project manager is active,
    /// updates and writes the project file.
    /// </summary>
    /// <param name="targetFolder">Optional target folder used for saving
    /// package resources; other resources will use relative paths.</param>
    /// <returns><c>true</c> when all resources were saved successfully.</returns>
    public static async Task<bool> SaveAllResourcesAsync(StorageFolder? targetFolder = null)
    {
        if (ResourceManager.Resources.Count > 0)
        {
            CurrentProject?.Entries?.Clear();

            bool allSaved = true;
            foreach (var resource in ResourceManager.Resources)
            {
                allSaved &= await SaveTopMostResourceAsync(resource);
            }

            if (UseProjectManager && CurrentProject != null)
            {
                allSaved &= await SaveProjectFileAsync();
            }

            return allSaved;
        }

        return false;
    }

    /// <summary>
    /// Saves a top-most resource and, when applicable, its children. Top-most
    /// resources are either direct children of <see cref="ProjectFolder"/>
    /// instances or direct children of the global resource list. When the
    /// project manager is enabled this method will also add project entries
    /// for saved resources.
    /// </summary>
    /// <param name="resource">Resource to save.</param>
    /// <param name="subFolder">Optional relative subfolder under the work
    /// folder used when saving package resources.</param>
    /// <returns><c>true</c> when the resource (and its children) were saved
    /// successfully.</returns>
    private static async Task<bool> SaveTopMostResourceAsync(IResource resource, string? subFolder = null)
    {
        if (resource == null) return false;

        // If there are no active project loaded, then fallback to ResourceHelper and let it handle.
        if (!UseProjectManager)
            return await ResourceHelper.SaveResourceAsync(resource);

        // Only top-most resources (direct children of ProjectFolders and
        //     direct children of ResourceManager.Resources) are be handled here.
        if (resource is ProjectFolder && resource.ChildrenResources != null)
        {
            string subFolderPath = ResourceManager.GetAbsoluteFolderPath(resource);
            bool allChildrenSaved = true;
            foreach (var child in resource.ChildrenResources)
            {
                allChildrenSaved &= await SaveTopMostResourceAsync(child, subFolderPath);
            }
            return allChildrenSaved;
        }
        else
        {
            string? extension = resource switch
            {
                MetadataResource => JsonFileExtension,
                FileResource fileResource => fileResource.FileExtension,
                _ => string.Empty
            };

            // NOTE: To prevent adding the resources which are in the root folder to the project file.
            // As they will be loaded regardless.
            if (resource.ParentResource is ProjectFolder)
                CurrentProject?.Entries?.Add(new ProjectEntry(subFolder ?? PathSeparator.ToString(), resource.Title + extension));

            // Only pass the targetFolder for PackageResources. For others, they will use the relative folder path
            StorageFolder? targetFolder = null;
            if (resource is PackageResource)
                targetFolder = await StorageHelper.GetSubFolderAsync(ResourceHelper.WorkFolder, subFolder, true);

            return await ResourceHelper.SaveResourceAsync(resource, targetFolder);
        }
    }

    /// <summary>
    /// Saves a resource and its nearest savable ancestor (for example a
    /// package or metadata resource that uses split metadata). This is used
    /// by the editor to persist a focused resource together with its parent
    /// metadata without saving unrelated siblings.
    /// </summary>
    /// <param name="resource">Resource to save together with its savable
    /// ancestor.</param>
    /// <returns><c>true</c> when the save operations succeeded.</returns>
    public static async Task<bool> SaveResourceAndAncestorAsync(IResource? resource)
    {
        // ProjectFolder holds no metadata to save.
        if (resource is ProjectFolder) return false;

        if (UseProjectManager && GetSavableResource(resource) is IMetadataResource savable)
        {
            StorageFolder? targetFolder = null;
            // Checks if there are any parent ProjectFolder.
            if (ResourceManager.TryGetParentOfType(savable, out ProjectFolder? folder))
            {
                string subFolderPath = ResourceManager.GetAbsoluteFolderPath(folder);
                targetFolder = await StorageHelper.GetSubFolderAsync(ResourceHelper.WorkFolder, subFolderPath, true);
            }

            // This is to save just the resource and its savable parent without affecting the siblings
            bool result = resource == savable || await ResourceHelper.SaveResourceAsync(resource, targetFolder, false);
            result &= await ResourceHelper.SaveResourceAsync(savable, targetFolder, false);
            return result;
        }

        // Rely on relative folder path
        return await ResourceHelper.SaveResourceAsync(resource);
    }

    /// <summary>
    /// Finds the nearest savable metadata resource for the provided
    /// resource. A savable resource is a <see cref="PackageResource"/>, a
    /// <see cref="MetadataResource"/> that uses split metadata, or an
    /// ancestor that satisfies those conditions.
    /// </summary>
    /// <param name="resource">Resource to inspect.</param>
    /// <returns>The savable metadata resource or <c>null</c> when none found.</returns>
    private static IMetadataResource? GetSavableResource(IResource? resource)
    {
        if (resource == null) return null;
        else if (resource is PackageResource package) return package;
        else if (resource is MetadataResource metadataResource && metadataResource.SplitMetadata) return metadataResource;
        else if (ResourceManager.TryGetSavableParent(resource, out IMetadataResource? parent)) return parent;

        return null;
    }

    /// <summary>
    /// Adds a single resource entry to the current project's entries and
    /// persists the project file. Only resources with a parent
    /// <see cref="ProjectFolder"/> are added.
    /// </summary>
    /// <param name="resource">The resource to add to the project.</param>
    /// <returns><c>true</c> when the project file was updated successfully.</returns>
    public static async Task<bool> AddProjectEntryAsync(IResource? resource)
    {
        // Only add the resource to the project file if it is not a ProjectFolder and there is an active project loaded.
        if (resource == null || resource is ProjectFolder ||
            CurrentProject == null || !UseProjectManager) return false;

        string? subFolderPath = ResourceManager.GetAbsoluteFolderPath(resource.ParentResource);
        string? extension = resource switch
        {
            MetadataResource => JsonFileExtension,
            FileResource fileResource => fileResource.FileExtension,
            _ => string.Empty
        };
        var entry = new ProjectEntry(subFolderPath, resource.Title + extension);
        CurrentProject.Entries.AddItemToListIfNotExists(entry);

        return await SaveProjectFileAsync();
    }

    /// <summary>
    /// Rebuilds the project's entries from the current resource tree and
    /// writes the project file to storage.
    /// </summary>
    /// <returns><c>true</c> when the project file was saved.</returns>
    public static async Task<bool> UpdateProjectFileAsync()
    {
        if (CurrentProject == null || !UseProjectManager) return false;
        CurrentProject.Entries?.Clear();
        foreach (var resource in ResourceManager.Resources)
        {
            await CreateProjectEntryAsync(resource);
        }

        return await SaveProjectFileAsync();
    }

    /// <summary>
    /// Adds project entries for the provided resource and its children when
    /// applicable. This method is used while building the project index.
    /// </summary>
    /// <param name="resource">Resource to create entries for.</param>
    /// <param name="subFolder">Relative subfolder associated with this
    /// resource.</param>
    private static async Task CreateProjectEntryAsync(IResource resource, string? subFolder = null)
    {
        if (resource == null || !UseProjectManager) return;

        if (resource is ProjectFolder && resource.ChildrenResources != null)
        {
            string subFolderPath = ResourceManager.GetAbsoluteFolderPath(resource);
            foreach (var child in resource.ChildrenResources)
            {
                await CreateProjectEntryAsync(child, subFolderPath);
            }
        }
        else if (resource.ParentResource is ProjectFolder)
        {
            string? extension = resource switch
            {
                MetadataResource => JsonFileExtension,
                FileResource fileResource => fileResource.FileExtension,
                _ => string.Empty
            };
            CurrentProject?.Entries.AddItemToListIfNotExists(new ProjectEntry(subFolder ?? PathSeparator.ToString(), resource.Title + extension));
        }
    }

    /// <summary>
    /// Saves the current project to a project file.
    /// </summary>
    /// <returns><c>true</c> on successful save.</returns>
    private static async Task<bool> SaveProjectFileAsync()
    {
        if (CurrentProject == null) return false;

        StorageFile? saveFile = await ResourceHelper.PickSaveFileAsync(CurrentProject.Name, ProjectFileExtension, "Project File");
        if (saveFile != null)
        {
            string xml = Project.Serialize(CurrentProject);
            return await StorageHelper.WriteToFileAsync(saveFile, xml);
        }

        return false;
    }

    /// <summary>
    /// Raised when the <see cref="CurrentProject"/> property changes.
    /// </summary>
    public static event EventHandler<Project?> CurrentProjectChanged;
}
