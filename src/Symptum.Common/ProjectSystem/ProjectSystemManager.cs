using Symptum.Common.Helpers;
using Symptum.Core.Management.Resources;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Common.ProjectSystem;

public class ProjectSystemManager
{
    public static bool UseProjectManager { get; set; } = false;

    public static Project? CurrentProject { get; set; }

    public static async Task<bool> OpenWorkFolderAsync(StorageFolder? folder = null)
    {
        bool result = await ResourceHelper.SelectWorkFolderAsync(folder);
        if (result && StorageHelper.IsFolderPickerSupported)
        {
            CurrentProject = null;
            UseProjectManager = false;
            ResourceManager.Resources.Clear();
            await ProcessFilesFromWorkPathAsync();
            return true;
        }

        return false;
    }

    private static async Task ProcessFilesFromWorkPathAsync()
    {
        var files = await ResourceHelper.GetFilesFromWorkPathAsync();
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            await ProcessFileAsync(file);
        }
    }

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
                    if (parent != null)
                        parent.AddChildResource(folder);
                    else
                        ResourceManager.Resources.Add(folder);

                    ((IResource)folder).InitializeResource(parent);
                }
                parent = folder;
            }
        }

        return parent;
    }

    private static async Task LoadProjectFromFileAsync(StorageFile? file)
    {
        if (file == null) return;

        string xml = await FileIO.ReadTextAsync(file);
        CurrentProject = Project.DeserializeProject(xml);
        if (CurrentProject != null && CurrentProject.Entries != null)
        {
            CurrentProject.Name = file.DisplayName;
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

            // NOTE: To prevent adding the resources to the project file.
            // As they will be loaded regardless.
            if (resource.ParentResource is ProjectFolder)
                CurrentProject?.Entries?.Add(new(subFolder ?? PathSeparator.ToString(), resource.Title + extension));

            // Only pass the targetFolder for PackageResources. For others, they will use the relative folder path
            StorageFolder? targetFolder = null;
            if (resource is PackageResource)
                targetFolder = await StorageHelper.CreateSubFoldersAsync(ResourceHelper.WorkFolder, subFolder);

            return await ResourceHelper.SaveResourceAsync(resource, targetFolder);
        }
    }

    // This will find the ancestor project folder and savable metadata and save it as well
    // (i.e. PackageResource or MetadataResource with SplitMetadata = true).
    // NOTE: wouldn't it be problematic if this function also saves the sibling resources?
    public static async Task<bool> SaveResourceAndAncestorAsync(IResource? resource)
    {
        if (UseProjectManager && ResourceManager.TryGetSavableParent(resource, out IMetadataResource? parent))
        {
            StorageFolder? targetFolder = null;
            // Checks if there are any parent ProjectFolder.
            if (ResourceManager.TryGetParentOfType(parent, out ProjectFolder? folder))
            {
                string subFolderPath = ResourceManager.GetAbsoluteFolderPath(folder);
                targetFolder = await StorageHelper.CreateSubFoldersAsync(ResourceHelper.WorkFolder, subFolderPath);
            }

            return await ResourceHelper.SaveResourceAsync(parent, targetFolder);
        }

        // Rely on relative folder path
        return await ResourceHelper.SaveResourceAsync(resource);
    }

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
}
