using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Subjects.QuestionBank;
using Symptum.Core.TypeConversion;
using Symptum.Editor.Controls;
using Windows.Storage.Pickers;

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

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        if (App.Current is App app && app.MainWindow is Window window)
        {
            hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            window.SetTitleBar(AppTitleBar);
            topicEditorDialog.XamlRoot = questionEditorDialog.XamlRoot = deleteTopicDialog.XamlRoot = window.Content.XamlRoot;
        }
#endif
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
    }

    private async void addTopicButton_Click(object sender, RoutedEventArgs e)
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

    private async void editTopicButton_Click(object sender, RoutedEventArgs e)
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

    private async void saveTopicsButton_Click(object sender, RoutedEventArgs e)
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
        foreach (var topic in topics)
        {
            var file = await workFolder.CreateFileAsync(topic.TopicName + ".csv", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, topic.ToCSV());
        }
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
        //{
        //    foreach (var topic in topics)
        //    {
        //        var fileSavePicker = new FileSavePicker
        //        {
        //            SuggestedStartLocation = PickerLocationId.ComputerFolder,
        //            SuggestedFileName = topic.TopicName + ".csv"
        //        };
        //        fileSavePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });

        //        StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
        //        if (saveFile != null)
        //        {
        //            CachedFileManager.DeferUpdates(saveFile);

        //            await FileIO.WriteTextAsync(saveFile, topic.ToCSV());

        //            await CachedFileManager.CompleteUpdatesAsync(saveFile);
        //        }
        //        else
        //        {

        //        }
        //    }
        //}
        //else
        //{

        //}
    }

    private async void deleteTopicsButton_Click(object sender, RoutedEventArgs e)
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
                    } catch { }

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

    private async void addQuestionButton_Click(object sender, RoutedEventArgs e)
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
            }
        }
    }

    private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = dataGrid.SelectedItems.Count;
        deleteQuestionsButton.IsEnabled = count > 0;
        duplicateQuestionButton.IsEnabled = count > 0;
        editQuestionButton.IsEnabled = count == 1;
    }

    private async void editQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        await EnterEditQuestionAsync();
    }

    private async void dataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await EnterEditQuestionAsync();
    }

    private async Task EnterEditQuestionAsync()
    {
        if (dataGrid.SelectedItems.Count == 0) return;
        if (dataGrid.SelectedItems[0] is QuestionEntry entry)
        {
            await questionEditorDialog.EditAsync(entry);
        }
    }

    private void duplicateQuestionButton_Click(object sender, RoutedEventArgs e)
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
    }

    private void deleteQuestionsButton_Click(object sender, RoutedEventArgs e)
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
    }

    private async Task LoadTopicsAsync()
    {
        if (workFolder == null) return;

        var files = await workFolder.GetFilesAsync();
        foreach (var file in files)
        {
            if (file.FileType.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
            {
                string csv = await FileIO.ReadTextAsync(file);
                var topic = QuestionBankTopic.CreateTopicFromCSV(file.DisplayName, csv);
                if (topic != null) topics.Add(topic);
            }
        }
    }

    private async void openFolderButton_Click(object sender, RoutedEventArgs e)
    {
        bool result = await SelectWorkPathAsync();
        if (result)
        {
            ResetData();
            topics.Clear();
            await LoadTopicsAsync();
        }
    }

    private async Task<bool> SelectWorkPathAsync()
    {
        FolderPicker folderPicker = new();
        folderPicker.FileTypeFilter.Add("*");

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);
#endif

        StorageFolder folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null && workFolder != folder)
        {
            workFolder = folder;
            WorkPathTextBlock.Text = $"• {workFolder.Path}";
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
        }
    }

    private void findQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (findFlyout == null)
        {
            List<string> columns =
            [
                nameof(QuestionEntry.Title),
                    nameof(QuestionEntry.Description),
                    nameof(QuestionEntry.ProbableCases)
            ];

            findFlyout = new()
            {
                FindContexts = columns,
                SelectedContext = columns[0],
            };

            findFlyout.QuerySubmitted += FindFlyout_QuerySubmitted;
        }

        FlyoutShowOptions flyoutShowOptions = new()
        {
            Position = new(ActualWidth, 80),
            Placement = FlyoutPlacementMode.Bottom
        };

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        findFlyout.XamlRoot = Content.XamlRoot;
#endif
        findFlyout.ShowAt(showOptions: flyoutShowOptions);
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
            case nameof(QuestionEntry.Description):
                {
                    return question.Description.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };
            case nameof(QuestionEntry.ProbableCases):
                {
                    string probableCases = ListToStringConversion.ConvertToString<string>(question.ProbableCases, x => x);
                    return probableCases.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };

            default: return false;
        }
    }
}
