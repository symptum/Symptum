using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Helpers;

namespace Symptum.Editor.Controls;

public sealed partial class QuestionEditorDialog : ContentDialog
{
    public QuestionEntry QuestionEntry { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private ObservableCollection<ListEditorItemWrapper<string>> descriptions = [];
    private ObservableCollection<ListEditorItemWrapper<DateOnly>> yearsAsked = [];
    private ObservableCollection<ListEditorItemWrapper<string>> probableCases = [];
    private ObservableCollection<ListEditorItemWrapper<BookReference>> bookReferences = [];
    private ObservableCollection<ListEditorItemWrapper<Uri>> linkReferences = [];

    private bool hasPreviouslyBeenAsked = true;
    private bool autoGenImp = true;

    private QuestionBankContext? context;

    public QuestionEditorDialog()
    {
        InitializeComponent();

        context ??= QuestionBankContextHelper.CurrentContext;

        qtCB.ItemsSource = Enum.GetValues(typeof(QuestionType));
        scCB.ItemsSource = Enum.GetValues(typeof(SubjectList));

        HandleListEditors();

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
        if (QuestionEntry == null)
        {
            scCB.SelectedItem = context?.SubjectCode;
            return;
        }

        qtCB.SelectedItem = QuestionEntry.Id?.QuestionType;
        scCB.SelectedItem = QuestionEntry.Id?.SubjectCode;
        cnTB.Text = QuestionEntry.Id?.CompetencyNumbers;
        titleTB.Text = QuestionEntry.Title;
        LoadLists(ref descriptions, QuestionEntry.Descriptions);
        paCB.IsChecked = hasPreviouslyBeenAsked = QuestionEntry.HasPreviouslyBeenAsked;
        importanceRC.Value = QuestionEntry.Importance;
        LoadLists(ref yearsAsked, QuestionEntry.YearsAsked);
        LoadLists(ref probableCases, QuestionEntry.ProbableCases);
        LoadLists(ref bookReferences, QuestionEntry.BookReferences);
        LoadLists(ref linkReferences, QuestionEntry.LinkReferences);
    }

    private void UpdateQuestionEntry()
    {
        QuestionEntry.Id ??= new();
        QuestionEntry.Id.QuestionType = qtCB.SelectedItem != null ? (QuestionType)qtCB.SelectedItem : QuestionType.Essay;
        QuestionEntry.Id.SubjectCode = scCB.SelectedItem != null ? (SubjectList)scCB.SelectedItem : SubjectList.None;
        QuestionEntry.Id.CompetencyNumbers = cnTB.Text;
        QuestionEntry.Title = titleTB.Text;
        QuestionEntry.Descriptions = GetUpdateList(descriptions);
        QuestionEntry.HasPreviouslyBeenAsked = hasPreviouslyBeenAsked;
        QuestionEntry.Importance = (int)importanceRC.Value;
        QuestionEntry.YearsAsked = GetUpdateList(yearsAsked);
        QuestionEntry.ProbableCases = GetUpdateList(probableCases);
        QuestionEntry.BookReferences = GetUpdateList(bookReferences);
        QuestionEntry.LinkReferences = GetUpdateList(linkReferences);
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
        LoadLists(ref probableCases, null);
        LoadLists(ref bookReferences, null);
        LoadLists(ref linkReferences, null);

        autoGenImpCB.IsChecked = autoGenImp = true;
    }

    private void LoadLists<T>(ref ObservableCollection<ListEditorItemWrapper<T>> destination, IList<T>? source)
    {
        destination.Clear();
        if (source == null || source.Count == 0) return;

        foreach (var item in source)
        {
            destination.Add(new ListEditorItemWrapper<T>(item));
        }
    }

    private List<T> GetUpdateList<T>(ObservableCollection<ListEditorItemWrapper<T>> source)
    {
        List<T> list = [];
        if (source == null || source.Count == 0) return list;

        foreach (var item in source)
        {
            list.Add(item.Value);
        }

        return list;
    }

    #region ListEditors Handling

    private void HandleListEditors()
    {
        #region Descriptions

        dsLE.ItemsSource = descriptions;
        dsLE.AddItemRequested += (s, e) => descriptions.Add(new ListEditorItemWrapper<string>(string.Empty));
        dsLE.ClearItemsRequested += (s, e) => descriptions.Clear();
        dsLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> description)
                descriptions.Remove(description);
        };
        dsLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> description)
                descriptions.Add(new() { Value = description.Value });
        };
        dsLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> description)
            {
                int oldIndex = descriptions.IndexOf(description);
                int newIndex = Math.Max(oldIndex - 1, 0);
                descriptions.Move(oldIndex, newIndex);
            }
        };
        dsLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> description)
            {
                int oldIndex = descriptions.IndexOf(description);
                int newIndex = Math.Min(oldIndex + 1, descriptions.Count - 1);
                descriptions.Move(oldIndex, newIndex);
            }
        };

        #endregion

        #region YearsAsked

        yaLE.ItemsSource = yearsAsked;
        yaLE.AddItemRequested += (s, e) =>
        {
            DateOnly date = context?.LastInputDate ?? DateOnly.FromDateTime(DateTime.Now);
            yearsAsked.Add(new ListEditorItemWrapper<DateOnly>(date));
            CalculateImportance();
        };
        yaLE.ClearItemsRequested += (s, e) =>
        {
            yearsAsked.Clear();
            CalculateImportance();
        };
        yaLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<DateOnly> date)
            {
                yearsAsked.Remove(date);
                CalculateImportance();
            }
        };
        yaLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<DateOnly> date)
            {
                yearsAsked.Add(new() { Value = date.Value });
                CalculateImportance();
            }
        };
        yaLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<DateOnly> date)
            {
                int oldIndex = yearsAsked.IndexOf(date);
                int newIndex = Math.Max(oldIndex - 1, 0);
                yearsAsked.Move(oldIndex, newIndex);
            }
        };
        yaLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<DateOnly> date)
            {
                int oldIndex = yearsAsked.IndexOf(date);
                int newIndex = Math.Min(oldIndex + 1, yearsAsked.Count - 1);
                yearsAsked.Move(oldIndex, newIndex);
            }
        };

        #endregion

        #region Probable Cases

        pcLE.ItemsSource = probableCases;
        pcLE.AddItemRequested += (s, e) => probableCases.Add(new ListEditorItemWrapper<string>(string.Empty));
        pcLE.ClearItemsRequested += (s, e) => probableCases.Clear();
        pcLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> @case)
                probableCases.Remove(@case);
        };
        pcLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> @case)
                probableCases.Add(new() { Value = @case.Value });
        };
        pcLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> @case)
            {
                int oldIndex = probableCases.IndexOf(@case);
                int newIndex = Math.Max(oldIndex - 1, 0);
                probableCases.Move(oldIndex, newIndex);
            }
        };
        pcLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<string> @case)
            {
                int oldIndex = probableCases.IndexOf(@case);
                int newIndex = Math.Min(oldIndex + 1, probableCases.Count - 1);
                probableCases.Move(oldIndex, newIndex);
            }
        };

        #endregion

        #region Book References

        brLE.ItemsSource = bookReferences;
        brLE.AddItemRequested += (s, e) =>
        {
            BookReference? bookReference = context?.PreferredBook;
            bookReferences.Add(new ListEditorItemWrapper<BookReference>(new() { Book = bookReference?.Book, Edition = bookReference?.Edition ?? 0, Volume = bookReference?.Volume ?? 0 }));
        };
        brLE.ClearItemsRequested += (s, e) => bookReferences.Clear();
        brLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<BookReference> bookReference)
                bookReferences.Remove(bookReference);
        };
        brLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<BookReference> x)
                bookReferences.Add(new() { Value = new() { Book = x.Value.Book, Edition = x.Value.Edition, Volume = x.Value.Volume, PageNumbers = x.Value.PageNumbers } });
        };
        brLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<BookReference> bookReference)
            {
                int oldIndex = bookReferences.IndexOf(bookReference);
                int newIndex = Math.Max(oldIndex - 1, 0);
                bookReferences.Move(oldIndex, newIndex);
            }
        };
        brLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<BookReference> bookReference)
            {
                int oldIndex = bookReferences.IndexOf(bookReference);
                int newIndex = Math.Min(oldIndex + 1, bookReferences.Count - 1);
                bookReferences.Move(oldIndex, newIndex);
            }
        };

        #endregion

        #region Link References

        lrLE.ItemsSource = linkReferences;
        lrLE.AddItemRequested += (s, e) => linkReferences.Add(new ListEditorItemWrapper<Uri>(ResourceManager.DefaultUri));
        lrLE.ClearItemsRequested += (s, e) => linkReferences.Clear();
        lrLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Uri> uri)
                linkReferences.Remove(uri);
        };
        lrLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Uri> uri)
                linkReferences.Add(new() { Value = uri.Value });
        };
        lrLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Uri> uri)
            {
                int oldIndex = linkReferences.IndexOf(uri);
                int newIndex = Math.Max(oldIndex - 1, 0);
                linkReferences.Move(oldIndex, newIndex);
            }
        };
        lrLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<Uri> uri)
            {
                int oldIndex = linkReferences.IndexOf(uri);
                int newIndex = Math.Min(oldIndex + 1, linkReferences.Count - 1);
                linkReferences.Move(oldIndex, newIndex);
            }
        };

        #endregion
    }

    #endregion
}
