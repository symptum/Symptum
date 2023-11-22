using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Subjects.QuestionBank;
using Symptum.Core.TypeConversion;
using Symptum.Editor.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Symptum.Editor
{
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<QuestionBankTopic> topics = new();

        private QuestionBankTopic currentTopic;

        private string workPath = string.Empty;

        private FindFlyout findFlyout;

        public MainWindow()
        {
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            treeView1.ItemsSource = topics;
            //LoadTopics();
        }

        private void treeView1_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is QuestionBankTopic topic)
            {
                currentTopic = topic;
                dataGrid.ItemsSource = topic.QuestionEntries;
                dataGrid.IsEnabled = true;
                addQuestionButton.IsEnabled = true;
                findQuestionButton.IsEnabled = true;
            }
        }

        private void treeView1_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            int count = sender.SelectedItems.Count;
            deleteTopicsButton.IsEnabled = count > 0;
            editTopicButton.IsEnabled = count == 1;
        }

        private async void addTopicButton_Click(object sender, RoutedEventArgs e)
        {
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
            if (treeView1.SelectedItems.Count == 0) return;
            if (treeView1.SelectedItems[0] is QuestionBankTopic topic)
            {
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
            if (string.IsNullOrEmpty(workPath) || !Directory.Exists(workPath))
            {
                pathExists = await SelectWorkPath();
            }
            else pathExists = true;

            if (pathExists)
                SaveCSVs();
        }

        private void SaveCSVs()
        {
            foreach (var topic in topics)
            {
                topic.SaveAsCSV(workPath + "\\" + topic.TopicName + ".csv");
            }
        }

        private async void deleteTopicsButton_Click(object sender, RoutedEventArgs e)
        {
            if (treeView1.SelectedItems.Count == 0) return;
            bool deleteCurrent = false;

            List<QuestionBankTopic> toDelete = new();

            var result = await deleteTopicDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                foreach (var item in treeView1.SelectedItems)
                {
                    if (item is QuestionBankTopic topic)
                    {
                        string csvfile = workPath + "\\" + topic.TopicName + ".csv";
                        if (File.Exists(csvfile))
                            File.Delete(csvfile);

                        toDelete.Add(topic);

                        if (item == currentTopic)
                            deleteCurrent = true;
                    }
                }

                treeView1.SelectedItems.Clear();
                toDelete.ForEach(x => topics.Remove(x));
                toDelete.Clear();

                ResetData(deleteCurrent);
            }
        }

        private async void addQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTopic != null)
            {
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

        private void LoadTopics()
        {
            if (!Directory.Exists(workPath)) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(workPath);
            var csvfiles = directoryInfo.GetFiles("*.csv");
            foreach (var csvfile in csvfiles)
            {
                var topic = QuestionBankTopic.ReadTopicFromCSV(csvfile.FullName);
                if (topic != null) topics.Add(topic);
            }
        }

        private async void openFolderButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await SelectWorkPath();
            if (result)
            {
                ResetData();
                topics.Clear();
                LoadTopics();
            }
        }

        private async Task<bool> SelectWorkPath()
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null && workPath != folder.Path)
            {
                workPath = folder.Path;
                WorkPathTextBlock.Text = $"• {workPath}";
                return true;
            }

            return false;
        }

        private void ResetData(bool currentTopicDeleted = true)
        {
            treeView1.SelectedItems.Clear();
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
                Position = new(AppWindow.Size.Width, 80),
                ShowMode = FlyoutShowMode.Standard,
                Placement = FlyoutPlacementMode.Bottom
            };

            findFlyout.XamlRoot = Content.XamlRoot;
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
}