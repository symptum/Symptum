using System.Globalization;
using System.Text;
using CsvHelper;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Controls;
using Symptum.Editor.EditorPages;
using Symptum.Editor.Helpers;
using Windows.Storage.Pickers;

namespace Symptum.Editor;

public sealed partial class MainPage : Page
{
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

        treeView.ItemsSource = ResourceManager.Resources;

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
        if (ResourceManager.Resources.Count == 0) return;

        bool pathExists = await ResourceHelper.VerifyWorkPathAsync();

        if (pathExists)
        {
            bool allSaved = true;
            foreach (var resource in ResourceManager.Resources)
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

    private async void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        bool result = await ResourceHelper.SelectWorkPathAsync();
        if (result && ResourceHelper.FolderPicked)
        {
            EditorPagesManager.ResetEditors();
            ResourceManager.Resources.Clear();
            await ResourceHelper.LoadResourcesFromWorkPathAsync();
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        IResource? parent = null;
        if (treeView.SelectedItems.Count > 0 && treeView.SelectedItems[0] is IResource resource)
            parent = resource;

        FileOpenPicker fileOpenPicker = new();
        fileOpenPicker.FileTypeFilter.Add(".csv");
        fileOpenPicker.FileTypeFilter.Add(".json");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hWnd);
#endif
        var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
        if (pickedFiles.Count > 0)
        {
            await ResourceHelper.LoadResourcesFromFilesAsync(pickedFiles, parent);
        }
    }

    private async void Markdown_Click(object sender, RoutedEventArgs e)
    {
        Dictionary<int, Dictionary<QuestionBankTopic, int>> totalW = [];
        List<QuestionBankTopic> topics = [];
        foreach (var resource in ResourceManager.Resources)
        {
            if (resource is QuestionBankTopic topic)
            {
                var weightages = topic.GenerateWeightage();
                foreach (var weightage in weightages)
                {
                    var year = weightage.Key;
                    if (totalW.TryGetValue(year, out Dictionary<QuestionBankTopic, int>? values))
                    {
                        values.Add(topic, weightage.Value);
                    }
                    else totalW.Add(year, new() { { topic, weightage.Value } });
                }
                topics.Add(topic);
            }
        }

        using var writer = new StringWriter();
        using var csvW = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csvW.WriteField("Year");
        foreach (var topic in topics)
        {
            csvW.WriteField(topic.Title);
        }
        csvW.NextRecord();
        //if (Entries != null)
        //{
        //    foreach (var entry in Entries)
        //    {
        //        csvW.WriteRecord(entry);
        //        csvW.NextRecord();
        //    }
        //}

        foreach (var w in totalW.OrderBy(x => x.Key))
        {
            csvW.WriteField(w.Key);
            if (w.Value is Dictionary<QuestionBankTopic, int> d)
            {
                foreach (var topic in topics)
                {
                    int value = 0;
                    foreach (var w2 in d)
                    {
                        if (w2.Key == topic)
                            value = w2.Value;
                    }
                    csvW.WriteField(value);
                }
            }
            csvW.NextRecord();
            //if (w.Value is Dictionary<QuestionBankTopic, int> d)
            //{
            //    foreach (var w2 in d)
            //    {
            //    }
            //}
        }

        System.Diagnostics.Debug.WriteLine(writer.ToString());

        return;

        if (_isBeingSaved)
        {
            return;
        }

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
                        ResourceManager.Resources.Add(instance);
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
