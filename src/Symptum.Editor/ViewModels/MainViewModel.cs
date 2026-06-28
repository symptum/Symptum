using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Common.Helpers;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Controls;
using Symptum.Editor.Pages;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Editor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string _resourcePaneTitle = "Resources";
    private const string _resourcePaneTitleFormat = "Resources - {0}";

    private readonly FileOpenPicker fileOpenPicker = new();
    private readonly AddNewItemDialog addNewItemDialog = new();
    private readonly DeleteItemsDialog deleteResourcesDialog = new()
    {
        Title = "Delete Resource(s)?",
        Content = "Do you want to delete the resources(s)?\nOnce you delete you won't be able to restore."
    };

    private XamlRoot? xamlRoot;
    private bool _isBeingSaved = false;
    private IList<IResource>? _selectedResources;

    #region Properties

    public static MainViewModel Instance { get; } = new();

    [ObservableProperty]
    public partial bool WorkFolderAvailable { get; private set; }

    [ObservableProperty]
    public partial string? WorkFolderName { get; private set; }

    [ObservableProperty]
    public partial string? WorkFolderPath { get; private set; }

    [ObservableProperty]
    public partial string? ResourcePaneTitle { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPackageCommand))]
    public partial IResource? SelectedResource { get; set; }

    [ObservableProperty]
    public partial bool ResourceViewMultiSelectionEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool DeleteButtonEnabled { get; set; } = false;

    public ICommand CloseAllEditorsCommand { get; } = new RelayCommand(EditorPagesManager.ResetEditors);

    public ICommand ExitApplicationCommand { get; } = new RelayCommand(Application.Current.Exit);

    [ObservableProperty]
    public partial IEditorPage? CurrentEditor { get; set; }

    #endregion

    private MainViewModel()
    { }

    public void Initialize()
    {
        // GenerateResources();
        xamlRoot = WindowHelper.MainWindow?.Content?.XamlRoot;
        ResourceHelper.WorkFolderChanged += WorkFolderChanged;
        ProjectSystemManager.CurrentProjectChanged += ProjectChanged;
        EditorPagesManager.SelectEditorRequested += SelectEditorRequested;
        fileOpenPicker.FileTypeFilter.Add(CsvFileExtension);
        fileOpenPicker.FileTypeFilter.Add(MarkdownFileExtension);
        fileOpenPicker.FileTypeFilter.Add(JsonFileExtension);
        fileOpenPicker.FileTypeFilter.AddRange(ImageFileExtensions);
        fileOpenPicker.FileTypeFilter.AddRange(AudioFileExtensions);
    }

    // private static void GenerateResources()
    // {
    //     for (int i = 0; i < 50; i++)
    //     {
    //         int catNum = i + 1;
    //         var category = new CategoryResource
    //         {
    //             Title = $"Category {catNum}",
    //         };

    //         for (int j = 0; j < 10; j++)
    //         {
    //             int subNum = j + 1;
    //             var sub = new MarkdownCategoryResource
    //             {
    //                 Title = $"MD Category {catNum}.{subNum}"
    //             };

    //             for (int k = 0; k < 20; k++)
    //             {
    //                 int itemNum = k + 1;
    //                 sub.AddChildResource(new MarkdownFileResource
    //                 {
    //                     Title = $"MD {catNum}.{subNum}.{itemNum}",
    //                 });
    //             }

    //             category.AddChildResource(sub);
    //         }
    //         ResourceManager.Resources.Add(category);
    //     }
    // }

    #region Resource View

    public void ResourceView_ResourcesSelected(object? s, IList<IResource>? selected)
    {
        _selectedResources = selected;
        UpdateDeleteButtonEnabled();
        if (_selectedResources?.Count > 0)
            SelectedResource = _selectedResources[0];
    }
    private void UpdateDeleteButtonEnabled() =>
        DeleteButtonEnabled = ResourceViewMultiSelectionEnabled && _selectedResources?.Count > 0;

    partial void OnResourceViewMultiSelectionEnabledChanged(bool value)
    {
        UpdateDeleteButtonEnabled();
    }

    public void ResourceView_ResourceOpenRequested(object? s, IResource? resource)
    {
        SelectedResource = resource;
        EditorPagesManager.CreateOrOpenEditor(resource);
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task LaunchWorkFolderAsync()
    {
#if WINDOWS && !HAS_UNO
        if (ResourceHelper.WorkFolder != null)
            await Windows.System.Launcher.LaunchFolderAsync(ResourceHelper.WorkFolder);
#endif
    }

    [RelayCommand]
    public async Task AddNewItemAsync(IResource? parent = null)
    {
        addNewItemDialog.XamlRoot = xamlRoot;
        var result = await addNewItemDialog.CreateAsync(parent);
        if (result == EditorResult.Create)
        {
            var selectedType = addNewItemDialog.SelectedItemType;
            if (selectedType != null)
            {
                if (Activator.CreateInstance(selectedType) is IResource instance)
                {
                    instance.Title = addNewItemDialog.ItemTitle;
                    if (parent != null)
                        parent.AddChildResource(instance);
                    else
                    {
                        ResourceManager.Resources.Add(instance);
                        instance.InitializeResource(null);
                    }
                }
            }
        }
    }

    [RelayCommand]
    public async Task CreateNewProjectAsync()
    {
        addNewItemDialog.XamlRoot = xamlRoot;
        var result = await addNewItemDialog.CreateProjectAsync();
        if (result == EditorResult.Create)
        {
            ProjectSystemManager.CurrentProject = new() { Name = addNewItemDialog.ItemTitle, Entries = [] };
            ProjectSystemManager.UseProjectManager = true;
        }
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
#if WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, WindowHelper.WindowHandle);
#endif
        // NOTE: Skia/X11: Picked files have URL encoded path and names.
        // It makes file names with " " unuseable.
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        if (pickedFiles.Count > 0)
        {
            await ResourceHelper.LoadResourcesFromFilesAsync(pickedFiles, SelectedResource);
        }
    }

    [RelayCommand]
    public async Task OpenWorkFolderAsync()
    {
        bool result = await ProjectSystemManager.OpenWorkFolderAsync();
        if (result)
        {
            EditorPagesManager.ResetEditors();
        }
    }

    [RelayCommand]
    public async Task SaveAllAsync()
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        EditorPagesManager.UpdateEditors();
        bool allSaved = await ProjectSystemManager.SaveAllResourcesAsync();
        if (allSaved) EditorPagesManager.MarkAllOpenEditorsAsSaved();

        _isBeingSaved = false;
    }

    [RelayCommand]
    public void CloseWorkFolder()
    {
        ProjectSystemManager.CurrentProject = null;
        EditorPagesManager.ResetEditors();
        ResourceHelper.CloseWorkFolder();
    }

    [RelayCommand]
    public async Task ImportPackageAsync()
    {
        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(PackageFileExtension);

#if WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, WindowHelper.WindowHandle);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        foreach (var file in pickedFiles)
        {
            await PackageHelper.ImportPackageAsync(file);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportResourceasPackage))]
    public async Task ExportPackageAsync() => await PackageHelper.ExportPackageAsync(SelectedResource as IPackageResource);

    private bool CanExportResourceasPackage() => SelectedResource is IPackageResource;

    [RelayCommand]
    public async Task DeleteResourcesAsync()
    {
        if (_selectedResources == null || _selectedResources.Count == 0) return;

        List<object> toDelete = [.. _selectedResources];
        if (toDelete.Count > 0)
        {
            deleteResourcesDialog.XamlRoot = xamlRoot;
            var result = await deleteResourcesDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                foreach (var item in toDelete)
                {
                    if (item is IResource resource)
                    {
                        await ResourceHelper.RemoveResourceAsync(resource, true);
                    }
                }
            }
        }

        _selectedResources.Clear();
    }

    [RelayCommand]
    public async Task DeleteResourceAsync(IResource? resource)
    {
        if (resource != null)
        {
            deleteResourcesDialog.XamlRoot = xamlRoot;
            var result = await deleteResourcesDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ResourceHelper.RemoveResourceAsync(resource, true);
            }
        }
    }

    #endregion

    #region Event Handling

    private void WorkFolderChanged(object? s, StorageFolder? dir)
    {
        WorkFolderAvailable = dir != null;
        WorkFolderName = dir?.DisplayName;
        WorkFolderPath = dir?.Path;
    }

    private void ProjectChanged(object? s, Project? project)
    {
        if (project == null || string.IsNullOrEmpty(project.Name))
            ResourcePaneTitle = _resourcePaneTitle;
        else
            ResourcePaneTitle = string.Format(_resourcePaneTitleFormat, project.Name);
    }

    private void SelectEditorRequested(object? s, IEditorPage? e) =>
        CurrentEditor = e;

    #endregion
}