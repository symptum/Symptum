using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Subjects.QuestionBank;
using Symptum.Core.TypeConversion;
using Symptum.Editor.Controls;
using Symptum.Editor.Helpers;
using Windows.Storage.Pickers;
using Windows.Storage.Pickers.Provider;

namespace Symptum.Editor;

public sealed partial class MainPage : Page
{
    private ObservableCollection<QuestionBankTopic> topics = new();

    private QuestionBankTopic currentTopic;

    private StorageFolder workFolder;

    private FindFlyout findFlyout;

    private IntPtr hWnd = IntPtr.Zero;
    private Window mainWindow;
    private TopicEditorDialog topicEditorDialog = new();
    private QuestionEditorDialog questionEditorDialog = new();

    private ContentDialog deleteTopicDialog = new()
    {
        Title = "Delete Topic(s)?",
        PrimaryButtonText = "Delete",
        CloseButtonText = "Cancel",
        Content = new TextBlock()
        {
            Text = "Do you want to delete the topic(s)? Once you delete you won't be able to restore."
        }
    };

    public MainPage()
    {
        this.InitializeComponent();

        if (App.Current is App app && app.MainWindow is Window window)
            mainWindow = window;

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        if (mainWindow != null)
        {
            hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            mainWindow.SetTitleBar(AppTitleBar);
            topicEditorDialog.XamlRoot = questionEditorDialog.XamlRoot = deleteTopicDialog.XamlRoot = mainWindow.Content.XamlRoot;
        }
        Background = null;
#endif
        TitleTextBlock.Text = mainWindow?.Title;

        topicsView.SelectionChanged += (s, e) =>
        {
            int count = topicsView.SelectedItems.Count;
            deleteTopicsButton.IsEnabled = count > 0;
            editTopicButton.IsEnabled = count == 1;
            if (count == 1 && topicsView.SelectedItems[0] is QuestionBankTopic topic)
            {
                SelectTopic(topic);
            }
        };
        topicsView.ItemsSource = topics;
    }

    private void SelectTopic(QuestionBankTopic topic)
    {
        currentTopic = topic;
        dataGrid.ItemsSource = topic.QuestionEntries;
        dataGrid.IsEnabled = true;
        addQuestionButton.IsEnabled = true;
        findQuestionButton.IsEnabled = true;
        SetCountsText();
    }

    private void SetCountsText(bool clear = false)
    {
        if (clear)
            countTextBlock.Text = null;
        else
            countTextBlock.Text = $"{currentTopic.QuestionEntries.Count} Entries, {dataGrid.SelectedItems.Count} Selected";
    }

    private async void AddTopicButton_Click(object sender, RoutedEventArgs e)
    {
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        topicEditorDialog.XamlRoot = Content.XamlRoot;
#endif
        var result = await topicEditorDialog.CreateAsync();
        if (result == EditResult.Save)
        {
            QuestionBankTopic topic = new(topicEditorDialog.TopicName)
            {
                QuestionEntries = new()
            };

            topics.Add(topic);
        }
    }

    private async void EditTopicButton_Click(object sender, RoutedEventArgs e)
    {
        if (topicsView.SelectedItems.Count == 0) return;
        if (topicsView.SelectedItems[0] is QuestionBankTopic topic)
        {
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            topicEditorDialog.XamlRoot = Content.XamlRoot;
#endif
            var result = await topicEditorDialog.EditAsync(topic.TopicName);
            if (result == EditResult.Save)
            {
                topic.TopicName = topicEditorDialog.TopicName;
            }
        }
    }

    private async void SaveTopicsButton_Click(object sender, RoutedEventArgs e)
    {
        bool pathExists;
        if (workFolder == null)
            pathExists = await SelectWorkPathAsync();
        else pathExists = true;

        if (pathExists)
            await SaveCSVsAsync();
    }

    private async Task SaveCSVsAsync()
    {
        if (_folderPicked)
        {
            foreach (var topic in topics)
            {
                var file = await workFolder.CreateFileAsync(topic.TopicName + ".csv", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, topic.ToCSV());
            }
        }
        else
        {
            foreach (var topic in topics)
            {
                var fileSavePicker = new FileSavePicker
                {
                    SuggestedFileName = topic.TopicName
                };
                fileSavePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
                WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hWnd);
#endif
                StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
                if (saveFile != null)
                {
                    CachedFileManager.DeferUpdates(saveFile);

                    await FileIO.WriteTextAsync(saveFile, topic.ToCSV());

                    await CachedFileManager.CompleteUpdatesAsync(saveFile);
                }
            }
        }
    }

    private async void DeleteTopicsButton_Click(object sender, RoutedEventArgs e)
    {
        if (topicsView.SelectedItems.Count == 0) return;
        bool deleteCurrent = false;

        List<QuestionBankTopic> toDelete = new();

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        deleteTopicDialog.XamlRoot = Content.XamlRoot;
#endif
        var result = await deleteTopicDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            foreach (var item in topicsView.SelectedItems)
            {
                if (item is QuestionBankTopic topic)
                {
                    try
                    {
                        var csvfile = await workFolder.GetFileAsync(topic.TopicName + ".csv");
                        if (csvfile != null) await csvfile.DeleteAsync();
                    }
                    catch { }

                    toDelete.Add(topic);

                    if (item == currentTopic)
                        deleteCurrent = true;
                }
            }

            topicsView.SelectedItems.Clear();
            toDelete.ForEach(x => topics.Remove(x));
            toDelete.Clear();

            ResetData(deleteCurrent);
        }
    }

    private async void AddQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentTopic != null)
        {
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            questionEditorDialog.XamlRoot = Content.XamlRoot;
#endif
            var result = await questionEditorDialog.CreateAsync();
            if (result == EditResult.Save)
            {
                currentTopic.QuestionEntries.Add(questionEditorDialog.QuestionEntry);
                SetCountsText();
            }
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = dataGrid.SelectedItems.Count;
        deleteQuestionsButton.IsEnabled = count > 0;
        duplicateQuestionButton.IsEnabled = count > 0;
        editQuestionButton.IsEnabled = count == 1;
        SetCountsText();
    }

    private async void EditQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        await EnterEditQuestionAsync();
    }

    private async void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await EnterEditQuestionAsync();
    }

    private async Task EnterEditQuestionAsync()
    {
        if (dataGrid.SelectedItems.Count == 0) return;
        if (dataGrid.SelectedItems[0] is QuestionEntry entry)
        {
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
            questionEditorDialog.XamlRoot = Content.XamlRoot;
#endif
            await questionEditorDialog.EditAsync(entry);
        }
    }

    private void DuplicateQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentTopic == null) return;
        List<QuestionEntry> toDupe = new();

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is QuestionEntry entry && currentTopic.QuestionEntries.Contains(entry))
                toDupe.Add(entry);
        }
        dataGrid.SelectedItems.Clear();
        toDupe.ForEach(x => currentTopic.QuestionEntries.Add(x.Clone()));
        toDupe.Clear();
        SetCountsText();
    }

    private void DeleteQuestionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentTopic == null) return;
        List<QuestionEntry> toDelete = new();

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is QuestionEntry entry && currentTopic.QuestionEntries.Contains(entry))
                toDelete.Add(entry);
        }
        dataGrid.SelectedItems.Clear();
        toDelete.ForEach(x => currentTopic.QuestionEntries.Remove(x));
        toDelete.Clear();
        SetCountsText();
    }

    private async Task LoadTopicsFromWorkPathAsync()
    {
        if (workFolder == null) return;

        var files = await workFolder.GetFilesAsync();
        await LoadTopicsFromFilesAsync(files);
    }

    private async Task LoadTopicsFromFilesAsync(IEnumerable<StorageFile> files)
    {
        foreach (StorageFile file in files)
        {
            if (file.FileType.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
            {
                string csv = await FileIO.ReadTextAsync(file);
                var topic = QuestionBankTopic.CreateTopicFromCSV(file.DisplayName, csv);
                if (topic != null) topics.Add(topic);
            }
        }
    }

    private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        bool result = await SelectWorkPathAsync();
        if (result && _folderPicked)
        {
            ResetData();
            topics.Clear();
            await LoadTopicsFromWorkPathAsync();
        }
    }

    private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
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

    private bool _folderPicked = false;

    private async Task<bool> SelectWorkPathAsync()
    {
        FolderPicker folderPicker = new();
        folderPicker.FileTypeFilter.Add("*");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
#endif

        StorageFolder folder;
        if (StorageHelper.IsFolderPickerSupported)
        {
            folder = await folderPicker.PickSingleFolderAsync();
            _folderPicked = true;
        }
        else
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            folder = await localFolder.CreateFolderAsync("Temp", CreationCollisionOption.OpenIfExists);
            _folderPicked = false;
        }

        if (folder != null && workFolder != folder)
        {
            workFolder = folder;
            mainWindow.Title = TitleTextBlock.Text = $"Symptum Editor • {workFolder.Path}";
            return true;
        }

        return false;
    }

    private void ResetData(bool currentTopicDeleted = true)
    {
        topicsView.SelectedItems.Clear();
        deleteTopicsButton.IsEnabled = false;
        editTopicButton.IsEnabled = false;
        if (currentTopicDeleted)
        {
            dataGrid.SelectedItems.Clear();
            dataGrid.ItemsSource = null;
            dataGrid.IsEnabled = false;
            addQuestionButton.IsEnabled = false;
            findQuestionButton.IsEnabled = false;
            currentTopic = null;
            SetCountsText(true);
        }
    }

    private void FindQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (findFlyout == null)
        {
            List<string> columns =
            [
                nameof(QuestionEntry.Title),
                    nameof(QuestionEntry.Descriptions),
                    nameof(QuestionEntry.ProbableCases)
            ];

            findFlyout = new()
            {
                FindContexts = columns,
                SelectedContext = columns[0],
            };

            findFlyout.QuerySubmitted += FindFlyout_QuerySubmitted;
        }

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        findFlyout.XamlRoot = Content.XamlRoot;
        FlyoutShowOptions flyoutShowOptions = new()
        {
            Position = new(ActualWidth, 80),
            Placement = FlyoutPlacementMode.Bottom
        };
        findFlyout.ShowAt(showOptions: flyoutShowOptions);
#else
        findFlyout.ShowAt(findQuestionButton);
#endif
    }

    private void FindFlyout_QuerySubmitted(object sender, FindFlyoutQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentTopic != null)
        {
            dataGrid.ItemsSource = new ObservableCollection<QuestionEntry>(from question in currentTopic.QuestionEntries.ToList()
                                                                           where QuestionEntryPropertyMatchValue(question, e)
                                                                           select question);
        }
    }

    // TODO: Implement Match Whole Word
    private bool QuestionEntryPropertyMatchValue(QuestionEntry question, FindFlyoutQuerySubmittedEventArgs e)
    {
        switch (e.Context)
        {
            case nameof(QuestionEntry.Title):
                {
                    return question.Title.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };
            case nameof(QuestionEntry.Descriptions):
                {
                    string descriptions = ListToStringConversion.ConvertToString<string>(question.Descriptions, x => x);
                    return descriptions.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };
            case nameof(QuestionEntry.ProbableCases):
                {
                    string probableCases = ListToStringConversion.ConvertToString<string>(question.ProbableCases, x => x);
                    return probableCases.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };

            default: return false;
        }
    }

    private void OpenSidePaneButton_Click(object sender, RoutedEventArgs e)
    {
        splitView.IsPaneOpen = true;
    }

    private void CloseSidePaneButton_Click(object sender, RoutedEventArgs e)
    {
        splitView.IsPaneOpen = false;
    }
}
