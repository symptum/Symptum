using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBanks;

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

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private ObservableCollection<ListItemWrapper<string>> descriptions = [];
    private ObservableCollection<ListItemWrapper<DateOnly>> yearsAsked = [];
    private ObservableCollection<ListItemWrapper<BookLocation>> bookLocations = [];
    private ObservableCollection<ListItemWrapper<string>> probableCases = [];
    private ObservableCollection<ListItemWrapper<Uri>> referenceLinks = [];

    private bool hasPreviouslyBeenAsked = true;
    private bool autoGenImp = true;

    public QuestionEditorDialog()
    {
        InitializeComponent();

        qtCB.ItemsSource = Enum.GetValues(typeof(QuestionType));
        scCB.ItemsSource = Enum.GetValues(typeof(SubjectList));

        dsLE.ItemsSource = descriptions;
        dsLE.AddItemRequested += (s, e) =>
        {
            descriptions.Add(new ListItemWrapper<string>(string.Empty));
        };
        dsLE.DeleteItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDelete)
            {
                if (item is ListItemWrapper<string> description)
                    descriptions.Remove(description);
            }
        };
        dsLE.DuplicateItemsRequested += (s, e) =>
        {
            foreach (var item in e.ItemsToDuplicate)
            {
                if (item is ListItemWrapper<string> description)
                    descriptions.Add(new() { Value = description.Value });
            }
        };

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
                    bookLocations.Add(new() { Value = new() { Book = x.Value.Book, Edition = x.Value.Edition, Volume = x.Value.Volume, PageNumber = x.Value.PageNumber } });
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
            autoGenImpCB.IsEnabled = true;
            yaLE.IsEnabled = true;
            EnableAutoGenerateImportance();
        };

        paCB.Unchecked += (s, e) =>
        {
            hasPreviouslyBeenAsked = false;
            autoGenImpCB.IsEnabled = false;
            DisableAutoGenerateImportance();
            yaLE.IsEnabled = false;
        };
        paCB.IsChecked = hasPreviouslyBeenAsked;

        autoGenImpCB.Checked += (s, e) =>
        {
            EnableAutoGenerateImportance();
        };
        autoGenImpCB.Unchecked += (s, e) =>
        {
            DisableAutoGenerateImportance();
        };

        Opened += QuestionEditorDialog_Opened;
        PrimaryButtonClick += QuestionEditorDialog_PrimaryButtonClick;
        CloseButtonClick += QuestionEditorDialog_CloseButtonClick;
    }

    private void EnableAutoGenerateImportance()
    {
        autoGenImp = autoGenImpCB.IsChecked ?? false;
        if (autoGenImp)
        {
            importanceRC.IsReadOnly = true;
            importanceRC.IsEnabled = false;
            CalculateImportance();
        }
    }

    private void DisableAutoGenerateImportance()
    {
        autoGenImp = false;
        importanceRC.IsReadOnly = false;
        importanceRC.IsEnabled = true;
    }

    private void CalculateImportance()
    {
        if (autoGenImp)
        {
            importanceRC.Value = Math.Min(yearsAsked.Count, 10);
        }
    }

    private void QuestionEditorDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
        ClearQuestionEntry();
    }

    private void QuestionEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = _isCreate ? EditorResult.Create : EditorResult.Update;
        QuestionEntry ??= new();

        UpdateQuestionEntry();
        ClearQuestionEntry();
    }

    private void QuestionEditorDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        LoadQuestionEntry();
    }

    private bool _isCreate = false;

    public async Task<EditorResult> CreateAsync()
    {
        Title = "Add a New Question";
        PrimaryButtonText = "Add";
        QuestionEntry = null;
        _isCreate = true;
        await ShowAsync();
        return EditResult;
    }

    public async Task<EditorResult> EditAsync(QuestionEntry questionEntry)
    {
        Title = "Edit Question";
        PrimaryButtonText = "Update";
        QuestionEntry = questionEntry;
        _isCreate = false;
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
        LoadLists(ref descriptions, QuestionEntry.Descriptions);
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
        QuestionEntry.Descriptions = GetUpdateList(descriptions);
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
        LoadLists(ref descriptions, null);
        paCB.IsChecked = hasPreviouslyBeenAsked = true;
        importanceRC.Value = 0;
        LoadLists(ref yearsAsked, null);
        LoadLists(ref bookLocations, null);
        LoadLists(ref probableCases, null);
        LoadLists(ref referenceLinks, null);

        autoGenImpCB.IsChecked = autoGenImp = true;
    }

    private void LoadLists<T>(ref ObservableCollection<ListItemWrapper<T>> destination, IList<T>? source)
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
        List<T> list = [];
        if (source == null || source.Count == 0) return list;

        foreach (var item in source)
        {
            list.Add(item.Value);
        }

        return list;
    }
}
