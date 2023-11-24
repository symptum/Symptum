using Microsoft.UI.Xaml.Controls;
using Symptum.Core.Management.Resource;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBank;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Symptum.Editor.Controls;

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
    private ObservableCollection<ListItemWrapper<string>> probableCases = new();
    private ObservableCollection<ListItemWrapper<Uri>> referenceLinks = new();

    private bool hasPreviouslyBeenAsked = true;

    public QuestionEditorDialog()
    {
        this.InitializeComponent();

        qtCB.ItemsSource = Enum.GetValues(typeof(QuestionType));
        scCB.ItemsSource = Enum.GetValues(typeof(SubjectList));

        yaLE.ItemsSource = yearsAsked;
        yaLE.AddItemRequested += (s, e) =>
        {
            yearsAsked.Add(new ListItemWrapper<DateOnly>(DateOnly.FromDateTime(DateTime.Now)));
            CalculateImportance();
        };
        yaLE.DeleteItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDelete)
            {
                if (item is ListItemWrapper<DateOnly> date)
                {
                    yearsAsked.Remove(date);
                }
            }
            CalculateImportance();
        };
        yaLE.DuplicateItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDuplicate)
            {
                if (item is ListItemWrapper<DateOnly> date)
                {
                    yearsAsked.Add(new() { Value = date.Value });
                }
            }
            CalculateImportance();
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
        blLE.DuplicateItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDuplicate)
            {
                if (item is ListItemWrapper<BookLocation> x)
                    bookLocations.Add(new() { Value = new() { Book = x.Value.Book, Edition = x.Value.Edition, Volume = x.Value.Volume, PageNumber = x.Value.PageNumber }});
            }
        };

        pcLE.ItemsSource = probableCases;
        pcLE.AddItemRequested += (s, e) =>
        {
            probableCases.Add(new ListItemWrapper<string>(string.Empty));
        };
        pcLE.DeleteItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDelete)
            {
                if (item is ListItemWrapper<string> @case)
                    probableCases.Remove(@case);
            }
        };
        pcLE.DuplicateItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDuplicate)
            {
                if (item is ListItemWrapper<string> @case)
                    probableCases.Add(new() { Value = @case.Value });
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
                if (item is ListItemWrapper<Uri> uri)
                    referenceLinks.Remove(uri);
            }
        };
        rlLE.DuplicateItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDuplicate)
            {
                if (item is ListItemWrapper<Uri> uri)
                    referenceLinks.Add(new() { Value = uri.Value });
            }
        };

        paCB.Checked += (s, e) =>
        {
            hasPreviouslyBeenAsked = true;
            importanceRC.IsReadOnly = true;
            importanceRC.IsEnabled = false;
            yaLE.IsEnabled = true;
            CalculateImportance();
        };

        paCB.Unchecked += (s, e) =>
        {
            hasPreviouslyBeenAsked = false;
            importanceRC.IsReadOnly = false;
            importanceRC.IsEnabled = true;
            yaLE.IsEnabled = false;
        };
        paCB.IsChecked = hasPreviouslyBeenAsked;

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

    private void CalculateImportance()
    {
        if (hasPreviouslyBeenAsked)
        {
            importanceRC.Value = Math.Min(yearsAsked.Count, 10);
        }
    }

    private void LoadQuestionEntry()
    {
        if (QuestionEntry == null) return;

        qtCB.SelectedItem = QuestionEntry.Id?.QuestionType;
        scCB.SelectedItem = QuestionEntry.Id?.SubjectCode;
        cnTB.Text = QuestionEntry.Id?.CompetencyNumbers;
        titleTB.Text = QuestionEntry.Title;
        descTB.Text = QuestionEntry.Description;
        paCB.IsChecked = hasPreviouslyBeenAsked = QuestionEntry.HasPreviouslyBeenAsked;
        importanceRC.Value = QuestionEntry.Importance;
        LoadLists(ref yearsAsked, QuestionEntry.YearsAsked);
        LoadLists(ref bookLocations, QuestionEntry.BookLocations);
        LoadLists(ref probableCases, QuestionEntry.ProbableCases);
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
        QuestionEntry.HasPreviouslyBeenAsked = hasPreviouslyBeenAsked;
        QuestionEntry.Importance = (int)importanceRC.Value;
        QuestionEntry.YearsAsked = GetUpdateList(yearsAsked);
        QuestionEntry.BookLocations = GetUpdateList(bookLocations);
        QuestionEntry.ProbableCases = GetUpdateList(probableCases);
        QuestionEntry.ReferenceLinks = GetUpdateList(referenceLinks);
    }

    private void ClearQuestionEntry()
    {
        qtCB.SelectedItem = null;
        scCB.SelectedItem = null;
        cnTB.Text = string.Empty;
        titleTB.Text = string.Empty;
        descTB.Text = string.Empty;
        paCB.IsChecked = hasPreviouslyBeenAsked = true;
        importanceRC.Value = 0;
        LoadLists(ref yearsAsked, null);
        LoadLists(ref bookLocations, null);
        LoadLists(ref probableCases, null);
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
