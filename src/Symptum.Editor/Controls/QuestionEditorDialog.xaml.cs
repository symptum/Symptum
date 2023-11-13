using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Symptum.Core.Management.Resource;
using Symptum.Core.QuestionBank;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Symptum.Editor.Controls
{
    public class ListItemWrapper<T>
    {
        public T Value { get; set; }

        public ListItemWrapper() { }

        public ListItemWrapper(T value)
        {
            Value = value;
        }
    }

    public sealed partial class QuestionEditorDialog : ContentDialog
    {
        public QuestionEntry QuestionEntry { get; private set; }

        public EditResult EditResult { get; private set; } = EditResult.None;

        private ObservableCollection<ListItemWrapper<DateOnly>> yearsAsked = new();
        private ObservableCollection<ListItemWrapper<string>> bookLocations = new();
        private ObservableCollection<ListItemWrapper<Uri>> referenceLinks = new();

        public QuestionEditorDialog()
        {
            this.InitializeComponent();

            yaLE.ItemsSource = yearsAsked;
            yaLE.AddItemRequested += (s, e) =>
            {
                yearsAsked.Add(new ListItemWrapper<DateOnly>(DateOnly.FromDateTime(DateTime.Now)));
            };
            yaLE.DeleteItemsRequested += (s, e) =>
            {
                foreach (var item in e.ItemsToDelete)
                {
                    if (item is ListItemWrapper<DateOnly> date)
                        yearsAsked.Remove(date);
                }
            };
            blLE.ItemsSource = bookLocations;
            blLE.AddItemRequested += (s, e) =>
            {
                bookLocations.Add(new ListItemWrapper<string>(string.Empty));
            };
            blLE.DeleteItemsRequested += (s, e) =>
            {
                foreach (var item in e.ItemsToDelete)
                {
                    if (item is ListItemWrapper<string> text)
                        bookLocations.Remove(text);
                }
            };
            rlLE.ItemsSource = referenceLinks;
            rlLE.AddItemRequested += (s, e) =>
            {
                referenceLinks.Add(new ListItemWrapper<Uri>(ResourceManager.DefaultUri));
            };
            rlLE.DeleteItemsRequested += (s, e) =>
            {
                foreach (var item in e.ItemsToDelete)
                {
                    if (item is ListItemWrapper<Uri> uris)
                        referenceLinks.Remove(uris);
                }
            };

            Opened += QuestionEditorDialog_Opened;
            PrimaryButtonClick += QuestionEditorDialog_PrimaryButtonClick;
            CloseButtonClick += QuestionEditorDialog_CloseButtonClick;
        }

        private void QuestionEditorDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            EditResult = EditResult.Cancel;
            ClearQuestionEntry();
        }

        private void QuestionEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            EditResult = EditResult.Save;
            if (QuestionEntry == null)
                QuestionEntry = new QuestionEntry();
            UpdateQuestionEntry();
            ClearQuestionEntry();
        }

        private void QuestionEditorDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            LoadQuestionEntry();
        }

        public async Task<EditResult> CreateAsync()
        {
            return await EditAsync(null, "Add a New Question");
        }

        public async Task<EditResult> EditAsync(QuestionEntry questionEntry, string dialogTitle = "Edit Question")
        {
            Title = dialogTitle;
            QuestionEntry = questionEntry;

            await ShowAsync();
            return EditResult;
        }

        private void LoadQuestionEntry()
        {
            if (QuestionEntry == null) return;

            idTB.Text = QuestionEntry.Id;
            titleTB.Text = QuestionEntry.Title;
            descTB.Text = QuestionEntry.Description;
            LoadLists(ref yearsAsked, QuestionEntry.YearsAsked);
            LoadLists(ref bookLocations, QuestionEntry.BookLocations);
            casesTB.Text = QuestionEntry.ProbableCases;
            LoadLists(ref referenceLinks, QuestionEntry.ReferenceLinks);
        }

        private void UpdateQuestionEntry()
        {
            QuestionEntry.Id = idTB.Text;
            QuestionEntry.Title = titleTB.Text;
            QuestionEntry.Description = descTB.Text;
            QuestionEntry.YearsAsked = GetUpdateList(yearsAsked);
            QuestionEntry.BookLocations = GetUpdateList(bookLocations);
            QuestionEntry.ProbableCases = casesTB.Text;
            QuestionEntry.ReferenceLinks = GetUpdateList(referenceLinks);
        }

        private void ClearQuestionEntry()
        {
            idTB.Text = string.Empty;
            titleTB.Text = string.Empty;
            descTB.Text = string.Empty;
            LoadLists(ref yearsAsked, null);
            LoadLists(ref bookLocations, null);
            casesTB.Text = string.Empty;
            LoadLists(ref referenceLinks, null);
        }

        private void LoadLists<T>(ref ObservableCollection<ListItemWrapper<T>> destination, IList<T> source)
        {
            destination.Clear();
            if (source == null || source.Count == 0) return;

            foreach (var item in source)
            {
                destination.Add(new ListItemWrapper<T>(item));
            }
        }

        private List<T> GetUpdateList<T>(ObservableCollection<ListItemWrapper<T>> source)
        {
            List<T> list = new();
            if (source == null || source.Count == 0) return list;

            foreach (var item in source)
            {
                list.Add(item.Value);
            }

            return list;
        }
    }
}
