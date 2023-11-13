using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Symptum.Core.QuestionBank;
using Symptum.Editor.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Symptum.Editor
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<QuestionBankTopic> topics = new ObservableCollection<QuestionBankTopic>();

        private QuestionBankTopic currentTopic;

        private string workPath = string.Empty;

        public MainWindow()
        {
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            this.Title = "Symptum Editor";
            treeView1.ItemsSource = topics;
            //topics.Add(QuestionBankManager.GetTestQuestionBankTopic());
        }

        private void treeView1_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is QuestionBankTopic topic)
            {
                currentTopic = topic;
                dataGrid.ItemsSource = topic.QuestionEntries;
            }
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

        private async void editQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItems.Count == 0) return;
            if (dataGrid.SelectedItems[0] is QuestionEntry entry)
            {
                await questionEditorDialog.EditAsync(entry);
            }
        }

        private async void saveTopicButton_Click(object sender, RoutedEventArgs e)
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
            foreach (var item in treeView1.SelectedItems)
            {
                if (item is QuestionBankTopic topic)
                {
                    topic.SaveAsCSV(workPath + "\\" + topic.TopicName + ".csv");
                }
            }
        }

        private async void deleteTopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (treeView1.SelectedItems.Count == 0) return;

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
                        topics.Remove(topic);
                    }
                }

                ResetData();
            }
        }

        private void LoadTopics()
        {
            if (!Directory.Exists(workPath)) return;

            DirectoryInfo directoryInfo = new DirectoryInfo(workPath);
            var csvfiles = directoryInfo.GetFiles("*.csv");
            foreach ( var csvfile in csvfiles)
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

        private void ResetData()
        {
            currentTopic = null;
            treeView1.SelectedItems.Clear();
            dataGrid.ItemsSource = null;
            dataGrid.SelectedItems.Clear();
        }
    }
}
