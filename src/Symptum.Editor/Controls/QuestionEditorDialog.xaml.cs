using Microsoft.UI.Xaml.Controls;
using Symptum.Core.Management.Resource;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBank;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Symptum.Editor.Controls
{
    public class ListItemWrapper<T>
    {
        public T Value { get; set; }

        public ListItemWrapper()
        { }

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
        private ObservableCollection<ListItemWrapper<BookLocation>> bookLocations = new();
        private ObservableCollection<ListItemWrapper<Uri>> referenceLinks = new();

        public QuestionEditorDialog()
        {
            this.InitializeComponent();

            qtCB.ItemsSource = Enum.GetValues(typeof(QuestionType));
            scCB.ItemsSource = Enum.GetValues(typeof(SubjectList));

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
                bookLocations.Add(new ListItemWrapper<BookLocation>(new BookLocation()));
            };
            blLE.DeleteItemsRequested += (s, e) =>
            {
                foreach (var item in e.ItemsToDelete)
                {
                    if (item is ListItemWrapper<BookLocation> bookLocation)
                        bookLocations.Remove(bookLocation);
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
            QuestionEntry ??= new();

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

            qtCB.SelectedItem = QuestionEntry.Id?.QuestionType;
            scCB.SelectedItem = QuestionEntry.Id?.SubjectCode;
            cnTB.Text = QuestionEntry.Id?.CompetencyNumbers;
            titleTB.Text = QuestionEntry.Title;
            descTB.Text = QuestionEntry.Description;
            LoadLists(ref yearsAsked, QuestionEntry.YearsAsked);
            LoadLists(ref bookLocations, QuestionEntry.BookLocations);
            casesTB.Text = QuestionEntry.ProbableCases;
            LoadLists(ref referenceLinks, QuestionEntry.ReferenceLinks);
        }

        private void UpdateQuestionEntry()
        {
            QuestionEntry.Id ??= new();
            QuestionEntry.Id.QuestionType = qtCB.SelectedItem != null ? (QuestionType)qtCB.SelectedItem : QuestionType.Essay;
            QuestionEntry.Id.SubjectCode = scCB.SelectedItem != null ? (SubjectList)scCB.SelectedItem : SubjectList.Anatomy;
            QuestionEntry.Id.CompetencyNumbers = cnTB.Text;
            QuestionEntry.Title = titleTB.Text;
            QuestionEntry.Description = descTB.Text;
            QuestionEntry.YearsAsked = GetUpdateList(yearsAsked);
            QuestionEntry.BookLocations = GetUpdateList(bookLocations);
            QuestionEntry.ProbableCases = casesTB.Text;
            QuestionEntry.ReferenceLinks = GetUpdateList(referenceLinks);
        }

        private void ClearQuestionEntry()
        {
            qtCB.SelectedItem = null;
            scCB.SelectedItem = null;
            cnTB.Text = string.Empty;
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