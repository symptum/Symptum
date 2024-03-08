using System.Collections.ObjectModel;
using System.Text;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Controls;
using Symptum.Editor.EditorPages;
using Symptum.Editor.Helpers;
using Windows.Storage.Pickers;

namespace Symptum.Editor;

public sealed partial class MainPage : Page
{
    private readonly ObservableCollection<IResource> resources = [];

    private IntPtr hWnd = IntPtr.Zero;
    private Window mainWindow;
    private readonly AddNewItemDialog addNewItemDialog = new();

    //private ContentDialog deleteTopicDialog = new()
    //{
    //    Title = "Delete Topic(s)?",
    //    PrimaryButtonText = "Delete",
    //    CloseButtonText = "Cancel",
    //    Content = new TextBlock()
    //    {
    //        Text = "Do you want to delete the topic(s)? Once you delete you won't be able to restore."
    //    }
    //};

    public MainPage()
    {
        InitializeComponent();

        //Subject subject = new(SubjectList.Anatomy)
        //{
        //    Title = "Anatomy",
        //    QuestionBank = new()
        //    {
        //        Title = "Anatomy Question Bank",
        //        QuestionBankPapers =
        //        [
        //            new("Paper 1")
        //            {
        //                Topics =
        //                [
        //                    new("Abdomen")
        //                    {
        //                        QuestionEntries =
        //                        [
        //                            new()
        //                            {
        //                                Title = "Liver"
        //                            }
        //                        ]
        //                    },
        //                    new("Upper Limb"),
        //                    new("Lower Limb")
        //                ]
        //            },
        //            new("Paper 2")
        //        ]
        //    }
        //};

        //using StreamReader sr = new(File.OpenRead("D:\\Symptum.Data\\Abdomen.csv"));
        //QuestionBankTopic topic = QuestionBankTopic.CreateTopicFromCSV("Abdomen", sr.ReadToEnd());

        //ObservableCollection<NavigableResource> items =
        //[
        //    subject,
        //    //topic
        //];

        //((IResource)subject).InitializeResource(null);
        //resources.Add(subject);
        //QuestionBankTopic topic = new("test")
        //{
        //    QuestionEntries =
        //    [
        //        new()
        //        {
        //            Title = "Liver"
        //        }
        //    ]
        //};
        //((IResource)topic).InitializeResource(null);
        //resources.Add(topic);

        treeView.ItemsSource = resources;

        if (App.Current is App app && app.MainWindow is Window window)
            mainWindow = window;

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        if (mainWindow != null)
        {
            hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            mainWindow.SetTitleBar(AppTitleBar);
            //topicEditorDialog.XamlRoot = questionEditorDialog.XamlRoot = deleteTopicDialog.XamlRoot = mainWindow.Content.XamlRoot;
        }
        Background = null;
        TitleTextBlock.Text = mainWindow?.Title;
#endif

        treeView.ItemInvoked += (s, e) =>
        {
            if (e.InvokedItem is IResource resource)
            {
                editorsTabView.SelectedItem = EditorPagesManager.CreateOrOpenEditorPage(resource);
            }
        };

        expandPaneButton.Click += (s, e) =>
        {
            splitView.IsPaneOpen = true;
        };

        editorsTabView.TabItemsSource = EditorPagesManager.EditorPages;
        ResourceHelper.Initialize(XamlRoot, hWnd);
    }

    //    private async void EditTopicButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (topicsView.SelectedItems.Count == 0) return;
    //        if (topicsView.SelectedItems[0] is QuestionBankTopic topic)
    //        {
    //#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
    //            topicEditorDialog.XamlRoot = Content.XamlRoot;
    //#endif
    //            var result = await topicEditorDialog.EditAsync(topic.Title);
    //            if (result == EditResult.Save)
    //            {
    //                topic.Title = topicEditorDialog.TopicName;
    //            }
    //        }
    //    }

    private bool _isBeingSaved = false;

    private async void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved)
        {
            return;
        }

        _isBeingSaved = true;
        if (resources.Count == 0) return;

        bool pathExists = await ResourceHelper.VerifyWorkPathAsync();

        if (pathExists)
        {
            bool allSaved = true;
            foreach (var resource in resources)
            {
                allSaved &= await ResourceHelper.SaveResourceAsync(resource);
            }
            if (allSaved) EditorPagesManager.MarkAllOpenEditorsAsSaved();
        }
        _isBeingSaved = false;
    }

    //    private async void DeleteTopicsButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (topicsView.SelectedItems.Count == 0) return;
    //        bool deleteCurrent = false;

    //        List<QuestionBankTopic> toDelete = [];

    //#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
    //        deleteTopicDialog.XamlRoot = Content.XamlRoot;
    //#endif
    //        var result = await deleteTopicDialog.ShowAsync();
    //        if (result == ContentDialogResult.Primary)
    //        {
    //            foreach (var item in topicsView.SelectedItems)
    //            {
    //                if (item is QuestionBankTopic topic)
    //                {
    //                    try
    //                    {
    //                        var csvfile = await workFolder.GetFileAsync(topic.Title + ".csv");
    //                        if (csvfile != null) await csvfile.DeleteAsync();
    //                    }
    //                    catch { }

    //                    toDelete.Add(topic);

    //                    if (item == currentTopic)
    //                        deleteCurrent = true;
    //                }
    //            }

    //            topicsView.SelectedItems.Clear();
    //            toDelete.ForEach(x => topics.Remove(x));
    //            toDelete.Clear();

    //            ResetData(deleteCurrent);
    //        }
    //    }

    private async Task LoadTopicsFromWorkPathAsync()
    {
        var files = await ResourceHelper.GetFilesFromWorkPathAsync();
        await LoadTopicsFromFilesAsync(files);
    }

    private async Task LoadTopicsFromFilesAsync(IEnumerable<StorageFile>? files)
    {
        if (files == null) return;

        foreach (StorageFile file in files)
        {
            if (file.FileType.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
            {
                string csv = await FileIO.ReadTextAsync(file);
                var topic = QuestionBankTopic.CreateTopicFromCSV(file.DisplayName, csv);
                if (topic != null) resources.Add(topic);
            }
        }
    }

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        bool result = await ResourceHelper.SelectWorkPathAsync();
        if (result && ResourceHelper.FolderPicked)
        {
            EditorPagesManager.ResetEditors();
            resources.Clear();
            await LoadTopicsFromWorkPathAsync();
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(".csv");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hWnd);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        if (pickedFiles.Count > 0)
        {
            await LoadTopicsFromFilesAsync(pickedFiles);
        }
    }

    private async void Markdown_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved)
        {
            return;
        }

        _isBeingSaved = true;

        StringBuilder mdBuilder = new();
        foreach (var resource in resources)
        {
            if (resource is QuestionBankTopic topic)
                MarkdownHelper.GenerateMarkdownForQuestionBankTopic(topic, ref mdBuilder);
        }
        var fileSavePicker = new FileSavePicker
        {
            SuggestedFileName = string.Empty
        };
        fileSavePicker.FileTypeChoices.Add("Markdown File", [".md"]);

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hWnd);
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
        EditorPagesManager.TryCloseEditorPage(args.Item as IEditorPage);
    }

    private async void New_Click(object sender, RoutedEventArgs e)
    {
//#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
//#endif
        addNewItemDialog.XamlRoot = mainWindow.Content?.XamlRoot;
        IResource? parent = null;
        if (treeView.SelectedItems.Count > 0 && treeView.SelectedItems[0] is IResource resource)
            parent = resource;
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
                        resources.Add(instance);
                        instance.InitializeResource(null);
                    }
                }
            }
            //QuestionBankTopic topic = new(addNewItemDialog.ItemTitle)
            //{
            //    QuestionEntries = []
            //};

            //topics.Add(topic);
        }
    }

    //    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    //    {
    //        FileOpenPicker fileOpenPicker = new();
    //        fileOpenPicker.FileTypeFilter.Add(".csv");

    //#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
    //        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hWnd);
    //#endif
    //        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
    //        if (pickedFiles.Count > 0)
    //        {
    //            await UpgradeCSVs(pickedFiles);
    //        }
    //    }

    //    private async Task UpgradeCSVs(IReadOnlyList<StorageFile> files)
    //    {
    //        if (files == null) return;

    //        foreach (StorageFile file in files)
    //        {
    //            if (file.FileType.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
    //            {
    //                string csv = await FileIO.ReadTextAsync(file);
    //                var newCSV = QuestionBankTopic.UpgradeCSV(csv);
    //                await FileIO.WriteTextAsync(file, newCSV);
    //            }
    //        }
    //    }
}
