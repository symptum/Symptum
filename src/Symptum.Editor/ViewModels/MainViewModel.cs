using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Common.Helpers;
using Symptum.Common.ProjectSystem;
using Symptum.Core.Data;
using Symptum.Core.Extensions;
using Symptum.Core.Management.Deployment;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.Editor.Pages;
using Windows.Storage.Pickers;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Editor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string _resourcePaneTitle = "Resources";
    private const string _resourcePaneTitleFormat = "Resources - {0}";

    private static StringBuilder _output = new();

    private readonly FileOpenPicker fileOpenPicker = new();
    private readonly AddNewItemDialog addNewItemDialog = new();

    private ConfirmationDialog? confirmationDialog;
    private EditAuthorInfoDialog? editAuthorInfoDialog;

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
    public partial string? ResourcePaneTitle { get; private set; } = _resourcePaneTitle;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPackageCommand))]
    public partial IResource? SelectedResource { get; set; }

    [ObservableProperty]
    public partial bool ResourceViewMultiSelectionEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool DeleteButtonEnabled { get; set; } = false;

    public ICommand CloseAllEditorsCommand { get; } = new RelayCommand(EditorPagesManager.ResetEditors);

    public ICommand CloseSavedEditorsCommand { get; } = new RelayCommand(EditorPagesManager.CloseSavedEditors);

    public ICommand ShowWelcomePageCommand { get; } = new RelayCommand(EditorPagesManager.ShowWelcomePage);

    public ICommand ExitApplicationCommand { get; } = new RelayCommand(Application.Current.Exit);

    [ObservableProperty]
    public partial IEditorPage? CurrentEditor { get; set; }

    [ObservableProperty]
    public partial AuthorInfo CurrentAuthor { get; set; }

    [ObservableProperty]
    public partial bool ShowResourcesPane { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowStatusBar { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowOutputPanel { get; set; } = true;

    [ObservableProperty]
    public partial string? OutputText { get; set; }

    public ObservableCollection<string> RecentItems { get; } = [];

    public bool HasRecentItems => RecentItems.Count > 0;

    #endregion

    #region Events

    public event Action? RecentItemsChanged;

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

        LoadSettings();
        AddOutputEntry("Session started");
    }

    public static void AddOutputEntry(string message, string? sender = null)
    {

        if (string.IsNullOrWhiteSpace(sender))
            _output.AppendLine($"[{DateTime.Now:hh:mm:ss}] - {message}");
        else
            _output.AppendLine($"[{DateTime.Now:hh:mm:ss}] - {sender}: {message}");

        Instance.OutputText = _output.ToString();
    }

    #region Settings

    private void LoadSettings()
    {
        CurrentAuthor = AuthorInfo.TryParse(EditorSettings.Author, out AuthorInfo author) ? author : new();
        ShowResourcesPane = EditorSettings.ShowResourcesPane;
        ShowStatusBar = EditorSettings.ShowStatusBar;
        ShowOutputPanel = EditorSettings.ShowOutputPanel;
        EditorSettings.LoadRecentItems(RecentItems);
        OnPropertyChanged(nameof(HasRecentItems));
        RecentItemsChanged?.Invoke();
    }

    partial void OnShowResourcesPaneChanged(bool value)
    {
        EditorSettings.ShowResourcesPane = value;
    }

    partial void OnShowStatusBarChanged(bool value)
    {
        EditorSettings.ShowStatusBar = value;
    }

    partial void OnShowOutputPanelChanged(bool value)
    {
        EditorSettings.ShowOutputPanel = value;
    }

    #endregion

    #region Recent Items

    private void AddRecentItem(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        RecentItems.RemoveItemFromListIfExists(path);
        RecentItems.Add(path);

        int count = RecentItems.Count;
        if (count > 10)
        {
            for (int i = 10; i < count; i++)
                RecentItems.RemoveAt(i);
        }

        EditorSettings.SaveRecentItems(RecentItems);
        RecentItemsChanged?.Invoke();
    }

    private void AddRecentItems(IReadOnlyList<StorageFile>? files)
    {
        if (files == null) return;
        var paths = files.Select(file => file.Path);
        RecentItems.AddRange(paths);

        int count = RecentItems.Count;
        if (count > 10)
        {
            for (int i = 10; i < count; i++)
                RecentItems.RemoveAt(i);
        }

        EditorSettings.SaveRecentItems(RecentItems);
        RecentItemsChanged?.Invoke();
    }

    private void RemoveRecentItem(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        RecentItems.RemoveItemFromListIfExists(path);
        EditorSettings.SaveRecentItems(RecentItems);
        RecentItemsChanged?.Invoke();
    }

    [RelayCommand]
    public void ClearRecentItems()
    {
        RecentItems.Clear();
        EditorSettings.SaveRecentItems(RecentItems);
        RecentItemsChanged?.Invoke();
    }

    #endregion

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

    #region Editors Tab View

    public async void EditorsTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is IEditorPage editor)
        {
            if (editor.HasUnsavedChanges && editor.EditableContent is IResource resource)
            {
                confirmationDialog ??= EditorPagesManager.CreateOrGetDialog<ConfirmationDialog>();
                confirmationDialog?.XamlRoot = xamlRoot;
                var result = await confirmationDialog.ConfirmClosingUnsavedAsync(resource.Title);
                if (result == EditorResult.Cancel)
                    return;
                if (result == EditorResult.Update)
                    await ProjectSystemManager.SaveResourceAndAncestorAsync(editor.EditableContent);
            }

            EditorPagesManager.TryCloseEditor(editor);
        }
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
                    EditorPagesManager.CreateOrOpenEditor(instance);
                    AddOutputEntry($"Created new {selectedType.Name}: {instance.Title}");
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
            AddOutputEntry($"Created new project: {addNewItemDialog.ItemTitle}");
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

            AddRecentItems(pickedFiles);
            AddOutputEntry($"Opened {pickedFiles.Count} file(s)");
        }
    }

    [RelayCommand]
    public async Task OpenWorkFolderAsync()
    {
        bool result = await ProjectSystemManager.OpenWorkFolderAsync();
        if (result)
        {
            EditorPagesManager.ResetEditors();
            if (ResourceHelper.WorkFolder != null)
            {
                AddRecentItem(ResourceHelper.WorkFolder.Path);
                AddOutputEntry($"Opened folder: {ResourceHelper.WorkFolder.Path}");
            }
        }
    }

    [RelayCommand]
    public async Task OpenProjectAsync()
    {
        FileOpenPicker projectPicker = new();
        projectPicker.FileTypeFilter.Add(ProjectFileExtension);

#if WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(projectPicker, WindowHelper.WindowHandle);
#endif
        var file = await projectPicker.PickSingleFileAsync();
        if (file != null)
        {
            StorageFolder? folder = await file.GetParentAsync();
            if (folder != null)
            {
                bool result = await ProjectSystemManager.OpenWorkFolderAsync(folder);
                if (result)
                {
                    EditorPagesManager.ResetEditors();
                    AddRecentItem(file.Path);
                    AddOutputEntry($"Opened project: {file.Path}");
                }
            }
        }
    }

    [RelayCommand]
    public async Task OpenRecentItemAsync(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (File.Exists(path))
            {
                StorageFile? file = await StorageFile.GetFileFromPathAsync(path);
                if (file != null)
                {
                    await ResourceHelper.LoadResourceFromFileAsync(file, SelectedResource);
                    AddOutputEntry($"Opened: {path}");
                }
            }
            else if (Directory.Exists(path))
            {
                StorageFolder? folder = await StorageFolder.GetFolderFromPathAsync(path);
                if (folder != null)
                {
                    await ProjectSystemManager.OpenWorkFolderAsync(folder);
                    AddOutputEntry($"Opened: {path}");
                }
            }
        }
        catch
        {
            RemoveRecentItem(path);
        }
    }

    [RelayCommand]
    public async Task SaveAllAsync()
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        EditorPagesManager.UpdateEditors();
        bool allSaved = await ProjectSystemManager.SaveAllResourcesAsync();
        if (allSaved)
        {
            EditorPagesManager.MarkAllOpenEditorsAsSaved();
            AddOutputEntry("Saved all resources");
        }

        _isBeingSaved = false;
    }

    [RelayCommand]
    public void CloseWorkFolder()
    {
        ProjectSystemManager.CurrentProject = null;
        EditorPagesManager.ResetEditors();
        ResourceHelper.CloseWorkFolder();
        AddOutputEntry("Closed work folder");
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
            AddOutputEntry($"Imported package: {file.Name}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportResourceasPackage))]
    public async Task ExportPackageAsync()
    {
        if (SelectedResource is IPackageResource package)
        {
            await PackageHelper.ExportPackageAsync(package);
            AddOutputEntry($"Exported package: {package.Title}");
        }
    }

    private bool CanExportResourceasPackage() => SelectedResource is IPackageResource;

    [RelayCommand]
    public async Task DeleteResourcesAsync()
    {
        if (_selectedResources == null || _selectedResources.Count == 0) return;

        List<object> toDelete = [.. _selectedResources];
        if (toDelete.Count > 0)
        {
            confirmationDialog ??= EditorPagesManager.CreateOrGetDialog<ConfirmationDialog>();
            confirmationDialog?.XamlRoot = xamlRoot;
            var result = await confirmationDialog?.ConfirmDeletionAsync("Resource(s)");
            if (result == EditorResult.Delete)
            {
                foreach (var item in toDelete)
                {
                    if (item is IResource resource)
                    {
                        await ResourceHelper.RemoveResourceAsync(resource, true);
                        EditorPagesManager.TryCloseEditorForResource(resource);
                    }
                }
                AddOutputEntry($"Deleted {toDelete.Count} resource(s)");
            }
        }

        _selectedResources.Clear();
    }

    [RelayCommand]
    public async Task DeleteResourceAsync(IResource? resource)
    {
        if (resource != null)
        {
            confirmationDialog ??= EditorPagesManager.CreateOrGetDialog<ConfirmationDialog>();
            confirmationDialog?.XamlRoot = xamlRoot;
            var result = await confirmationDialog.ConfirmDeletionAsync("Resource");
            if (result == EditorResult.Delete)
            {
                await ResourceHelper.RemoveResourceAsync(resource, true);
                EditorPagesManager.TryCloseEditorForResource(resource);
                AddOutputEntry($"Deleted: {resource.Title}");
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

        if (dir?.Path != null)
            EditorSettings.LastWorkFolderPath = dir.Path;
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

    [RelayCommand]
    public async Task EditAuthorInfoAsync()
    {
        editAuthorInfoDialog ??= new();
        editAuthorInfoDialog.XamlRoot = xamlRoot;

        var result = await editAuthorInfoDialog.EditAsync(CurrentAuthor);
        if (result == EditorResult.Update)
        {
            CurrentAuthor = editAuthorInfoDialog.Author;
            EditorSettings.Author = CurrentAuthor.ToString();
        }
    }

    #endregion
}
