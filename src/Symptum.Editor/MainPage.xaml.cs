using Symptum.Core.Management.Resources;
using Symptum.Common.Helpers;
using Symptum.Editor.Helpers;
using Symptum.Editor.Controls;
using Symptum.Editor.EditorPages;
using Windows.Storage.Pickers;
using Windows.System;
using System.Text;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Core.Management.Deployment;
using static Symptum.Core.Helpers.FileHelper;
using CsvHelper;
using System.Globalization;
using Symptum.Core.Extensions;

namespace Symptum.Editor;

public sealed partial class MainPage : Page
{
    private bool _collapsed = false;
    private bool _showResourcesPane = true;
    private readonly AddNewItemDialog addNewItemDialog = new();
    private readonly QuestionBankContextConfigureDialog contextConfigureDialog = new();

    private DeleteItemsDialog deleteResourceDialog = new()
    {
        Title = "Delete Resource(s)?",
        Content = "Do you want to delete the resources(s)?\nOnce you delete you won't be able to restore."
    };

    public MainPage()
    {
        InitializeComponent();

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO

        if (WindowHelper.MainWindow is Window mainWindow)
        {
            mainWindow.SetTitleBar(AppTitleBar);
            TitleTextBlock.Text = mainWindow.Title;
        }

        Background = null;

        workFolderButton.Click += async (s, e) =>
        {
            if (ResourceHelper.WorkFolder != null)
                await Launcher.LaunchFolderAsync(ResourceHelper.WorkFolder);
        };

#endif

        treeView.ItemsSource = ResourceManager.Resources;

        ResourceHelper.WorkFolderChanged += (s, e) =>
        {
            workFolderText.Text = e?.DisplayName;
            ToolTipService.SetToolTip(workFolderButton, e?.Path);
            workFolderButton.Visibility = e != null ? Visibility.Visible : Visibility.Collapsed;
        };

        treeView.SelectionChanged += (_, _) => UpdateDeleteButtonEnabled();

        treeView.ItemInvoked += (s, e) =>
        {
            if (e.InvokedItem is IResource resource)
            {
                EditorPagesManager.CreateOrOpenEditor(resource);
            }
        };

        expandResourcesPaneButton.Click += (s, e) =>
        {
            splitView.IsPaneOpen = true;
        };

        EditorPagesManager.CurrentEditorChanged += (s, e) => editorsTabView.SelectedItem = e;

        SizeChanged += MainPage_SizeChanged;
    }

    private void MainPage_SizeChanged(object sender, SizeChangedEventArgs args)
    {
        bool collapsed = args.NewSize.Width switch
        {
            >= 1007 => false,
            _ => true
        };

        if (collapsed != _collapsed)
        {
            _collapsed = collapsed;
            VisualStateManager.GoToState(this, collapsed || !_showResourcesPane ? "MinimalState" : "DefaultState", true);
        }
    }

    private async void Markdown_Click(object sender, RoutedEventArgs e)
    {
        //Dictionary<int, Dictionary<QuestionBankTopic, int>> totalW = [];
        //List<QuestionBankTopic> topics = [];
        //foreach (var resource in ResourceManager.Resources)
        //{
        //    if (resource is QuestionBankTopic topic)
        //    {
        //        var weightages = topic.GenerateWeightage();
        //        foreach (var weightage in weightages)
        //        {
        //            var year = weightage.Key;
        //            if (totalW.TryGetValue(year, out Dictionary<QuestionBankTopic, int>? values))
        //            {
        //                values.Add(topic, weightage.Value);
        //            }
        //            else totalW.Add(year, new() { { topic, weightage.Value } });
        //        }
        //        topics.Add(topic);
        //    }
        //}

        //using var writer = new StringWriter();
        //using var csvW = new CsvWriter(writer, CultureInfo.InvariantCulture);

        //csvW.WriteField("Year");
        //foreach (var topic in topics)
        //{
        //    csvW.WriteField(topic.Title);
        //}
        //csvW.NextRecord();
        ////if (Entries != null)
        ////{
        ////    foreach (var entry in Entries)
        ////    {
        ////        csvW.WriteRecord(entry);
        ////        csvW.NextRecord();
        ////    }
        ////}

        //foreach (var w in totalW.OrderBy(x => x.Key))
        //{
        //    csvW.WriteField(w.Key);
        //    if (w.Value is Dictionary<QuestionBankTopic, int> d)
        //    {
        //        foreach (var topic in topics)
        //        {
        //            int value = 0;
        //            foreach (var w2 in d)
        //            {
        //                if (w2.Key == topic)
        //                    value = w2.Value;
        //            }
        //            csvW.WriteField(value);
        //        }
        //    }
        //    csvW.NextRecord();
        //    //if (w.Value is Dictionary<QuestionBankTopic, int> d)
        //    //{
        //    //    foreach (var w2 in d)
        //    //    {
        //    //    }
        //    //}
        //}

        //System.Diagnostics.Debug.WriteLine(writer.ToString());

        //return;

        if (_isBeingSaved) return;

        _isBeingSaved = true;

        StringBuilder mdBuilder = new();
        foreach (var resource in ResourceManager.Resources)
        {
            if (resource is QuestionBankTopic topic)
                MarkdownHelper.GenerateMarkdownForQuestionBankTopic(topic, ref mdBuilder);
        }
        var fileSavePicker = new FileSavePicker
        {
            SuggestedFileName = string.Empty
        };
        fileSavePicker.FileTypeChoices.Add("Markdown File", [MarkdownFileExtension]);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, WindowHelper.WindowHandle);
#endif
        StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
        if (saveFile != null)
        {
            CachedFileManager.DeferUpdates(saveFile);

            await FileIO.WriteTextAsync(saveFile, mdBuilder.ToString());

            await CachedFileManager.CompleteUpdatesAsync(saveFile);
        }
        _isBeingSaved = false;
    }

    private void EditorsTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        EditorPagesManager.TryCloseEditor(args.Item as IEditorPage);
    }

    private async void New_Click(object sender, RoutedEventArgs e)
    {
        await AddNewItemAsync();
    }

    private async Task AddNewItemAsync(IResource? parent = null)
    {
        addNewItemDialog.XamlRoot = WindowHelper.MainWindow?.Content?.XamlRoot;
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

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        IResource? parent = null;
        if (treeView.SelectedItems.Count > 0 && treeView.SelectedItems[0] is IResource resource)
            parent = resource;

        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(CsvFileExtension);
        fileOpenPicker.FileTypeFilter.Add(MarkdownFileExtension);
        fileOpenPicker.FileTypeFilter.Add(JsonFileExtension);
        fileOpenPicker.FileTypeFilter.AddRange(ImageFileExtensions);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, WindowHelper.WindowHandle);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        if (pickedFiles.Count > 0)
        {
            await ResourceHelper.LoadResourcesFromFilesAsync(pickedFiles, parent);
        }
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        bool result = await ResourceHelper.OpenWorkFolderAsync();
        if (result)
        {
            EditorPagesManager.ResetEditors();
        }
    }

    private bool _isBeingSaved = false;

    private async void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        bool allSaved = await ResourceHelper.SaveAllResourcesAsync();
        if (allSaved) EditorPagesManager.MarkAllOpenEditorsAsSaved();

        _isBeingSaved = false;
    }

    private void CloseFolder_Click(object sender, RoutedEventArgs e)
    {
        EditorPagesManager.ResetEditors();
        ResourceHelper.CloseWorkFolder();
    }

    private async void ImportPackage_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(PackageFileExtension);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, WindowHelper.WindowHandle);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        foreach (var file in pickedFiles)
        {
            await PackageHelper.ImportPackageAsync(file);
        }
    }

    private async void ExportPackage_Click(object sender, RoutedEventArgs e)
    {
        if (treeView.SelectedItems.Count > 0 && treeView.SelectedItems[0] is IPackageResource package)
            await PackageHelper.ExportPackageAsync(package);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private async void ConfigureContext_Click(object sender, RoutedEventArgs e)
    {
        contextConfigureDialog.XamlRoot = WindowHelper.MainWindow?.Content?.XamlRoot;
        await contextConfigureDialog.ShowAsync();
    }

    private void MultiSelectButton_Click(object sender, RoutedEventArgs e)
    {
        treeView.SelectionMode = multiSelectButton.IsChecked switch
        {
            true => TreeViewSelectionMode.Multiple,
            false => TreeViewSelectionMode.Single,
            _ => TreeViewSelectionMode.None
        };
        UpdateDeleteButtonEnabled();
    }

    private void ShowResourcesPaneButton_Click(object sender, RoutedEventArgs e)
    {
        _showResourcesPane = !_showResourcesPane;
        ToolTipService.SetToolTip(showResourcesPaneButton, _showResourcesPane ? "Unpin" : "Pin");
        resourcesPaneButtonSymbolIcon.Symbol = _showResourcesPane ? Symbol.UnPin : Symbol.Pin;
        VisualStateManager.GoToState(this, _showResourcesPane && !_collapsed ? "DefaultState" : "MinimalState", true);
    }

    private void UpdateDeleteButtonEnabled()
    {
        int count = treeView.SelectedItems.Count;
        deleteResourcesButton.IsEnabled = (multiSelectButton.IsChecked ?? false) && count > 0;
    }

    private async void DeleteResourcesButton_Click(object sender, RoutedEventArgs e)
    {
        IList<object> toDelete = treeView.SelectedItems.ToList();
        if (toDelete.Count > 0)
        {
            deleteResourceDialog.XamlRoot = WindowHelper.MainWindow?.Content?.XamlRoot;
            var result = await deleteResourceDialog.ShowAsync();
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
        treeView.SelectedItems.Clear();
    }

    private async void DeleteFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var context = (sender as MenuFlyoutItem)?.DataContext;
        if (context is IResource resource)
        {
            deleteResourceDialog.XamlRoot = WindowHelper.MainWindow?.Content?.XamlRoot;
            var result = await deleteResourceDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ResourceHelper.RemoveResourceAsync(resource, true);
            }
        }
    }

    private async void AddNewFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var context = (sender as MenuFlyoutItem)?.DataContext;
        if (context is IResource parent)
        {
            await AddNewItemAsync(parent);
        }
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(CsvFileExtension);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, WindowHelper.WindowHandle);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        if (pickedFiles.Count > 0)
        {
            foreach (StorageFile file in pickedFiles)
            {
                if (file != null && file.FileType.Equals(CsvFileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    QuestionBankTopic topic = new();
                    string csv = await FileIO.ReadTextAsync(file);
                    if (string.IsNullOrEmpty(csv)) return;

                    using StringReader reader = new(csv);
                    using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
                    var entries = csvReader.GetRecords<QuestionEntryO>().ToList();

                    List<QuestionEntry> entriesn = [];
                    foreach (var entry in entries)
                    {
                        QuestionEntry entry1 = new()
                        {
                            Id = new() { QuestionType = entry.Id.QuestionType, SubjectCode = entry.Id.SubjectCode, CompetencyNumbers = entry.Id.CompetencyNumbers },
                            Title = entry.Title,
                            Descriptions = entry.Descriptions.CloneList(),
                            HasPreviouslyBeenAsked = entry.HasPreviouslyBeenAsked,
                            Importance = entry.Importance,
                            YearsAsked = entry.YearsAsked.CloneList(),
                            ProbableCases = entry.ProbableCases.CloneList()
                        };

                        if (entry.BookReferences?.Count > 0)
                        {
                            entry1.References = new(entry.BookReferences);
                        }
                        entriesn.Add(entry1);
                    }

                    topic.Entries = new(entriesn);

                    using StringWriter writer = new();
                    using CsvWriter csvW = new(writer, CultureInfo.InvariantCulture);
                    csvW.WriteHeader<QuestionEntry>();
                    csvW.NextRecord();
                    if (entriesn != null)
                    {
                        foreach (var entry in entriesn)
                        {
                            csvW.WriteRecord(entry);
                            csvW.NextRecord();
                        }
                    }

                    await FileIO.WriteTextAsync(file, writer.ToString());
                }
            }
        }
    }
}
