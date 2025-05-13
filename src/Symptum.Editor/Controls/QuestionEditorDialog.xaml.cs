using System.Collections.ObjectModel;
using Symptum.Core.Data.Bibliography;
using Symptum.Core.Subjects;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Helpers;

namespace Symptum.Editor.Controls;

public sealed partial class QuestionEditorDialog : ContentDialog
{
    public QuestionEntry? QuestionEntry { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private readonly ObservableCollection<ListEditorItemWrapper<string>> descriptions = [];
    private readonly ObservableCollection<ListEditorItemWrapper<DateOnly>> yearsAsked = [];
    private readonly ObservableCollection<ListEditorItemWrapper<string>> probableCases = [];
    private readonly ObservableCollection<ListEditorItemWrapper<ReferenceBase>> references = [];

    private bool hasPreviouslyBeenAsked = true;
    private bool autoGenImp = true;

    private QuestionBankContext? context;

    public QuestionEditorDialog()
    {
        InitializeComponent();

        context ??= QuestionBankContextHelper.CurrentContext;

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
        UpdateQuestionEntry();
        ClearQuestionEntry();
    }

    private void QuestionEditorDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        LoadQuestionEntry();
    }

    private bool _isCreate = false;
    private QuestionType _questionType = QuestionType.ShortNote;

    public async Task<EditorResult> CreateAsync(QuestionType? questionType = null)
    {
        Title = "Add a New Question";
        PrimaryButtonText = "Add";
        QuestionEntry = null;
        _isCreate = true;
        if (questionType.HasValue) _questionType = questionType.Value;
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
            qtCB.SelectedItem = _questionType;
            // Load new question entry with context's subject code, selected year and book.
            scCB.SelectedItem = context?.SubjectCode;
            if (context?.LastInputDate is DateOnly date)
            {
                yearsAsked.Add(new ListEditorItemWrapper<DateOnly>(date));
                CalculateImportance();
            }
            if (context?.PreferredBook is PresetBookReference bookReference)
                references.Add(new() { Value = bookReference });

            return;
        }

        qtCB.SelectedItem = QuestionEntry.Id?.QuestionType;
        scCB.SelectedItem = QuestionEntry.Id?.SubjectCode;
        //cnTB.Text = QuestionEntry.Id?.CompetencyNumbers;
        titleTB.Text = QuestionEntry.Title;
        descriptions.LoadFromList(QuestionEntry.Descriptions);
        paCB.IsChecked = hasPreviouslyBeenAsked = QuestionEntry.HasPreviouslyBeenAsked;
        importanceRC.Value = QuestionEntry.Importance;
        yearsAsked.LoadFromList(QuestionEntry.YearsAsked);
        probableCases.LoadFromList(QuestionEntry.ProbableCases);
        references.LoadFromList(QuestionEntry.References);
    }

    private void UpdateQuestionEntry()
    {
        QuestionEntry ??= new();
        QuestionEntry.Id ??= new();
        QuestionEntry.Id.QuestionType = qtCB.SelectedItem != null ? (QuestionType)qtCB.SelectedItem : QuestionType.Essay;
        QuestionEntry.Id.SubjectCode = scCB.SelectedItem != null ? (SubjectList)scCB.SelectedItem : SubjectList.None;
        //QuestionEntry.Id.CompetencyNumbers = cnTB.Text;
        QuestionEntry.Title = titleTB.Text;
        QuestionEntry.Descriptions = descriptions.UnwrapToList();
        QuestionEntry.HasPreviouslyBeenAsked = hasPreviouslyBeenAsked;
        QuestionEntry.Importance = (int)importanceRC.Value;
        QuestionEntry.YearsAsked = yearsAsked.UnwrapToList();
        QuestionEntry.ProbableCases = probableCases.UnwrapToList();
        QuestionEntry.References = references.UnwrapToList();
    }

    private void ClearQuestionEntry()
    {
        qtCB.SelectedItem = null;
        scCB.SelectedItem = null;
        //cnTB.Text = string.Empty;
        titleTB.Text = string.Empty;
        descriptions.Clear();
        paCB.IsChecked = hasPreviouslyBeenAsked = true;
        importanceRC.Value = 0;
        yearsAsked.Clear();
        probableCases.Clear();
        references.Clear();

        autoGenImpCB.IsChecked = autoGenImp = true;
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

        #region References

        rfLE.ItemsSource = references;
        rfLE.ItemTypes =
        [
            new ("Preset Book Reference", typeof(PresetBookReference)),
            new ("Journal Article Reference", typeof(JournalArticleReference)),
            new ("Link Reference", typeof(LinkReference)),
        ];

        rfLE.ItemsSource = references;
        rfLE.AddItemRequested += (s, e) =>
        {
            if (e == typeof(PresetBookReference))
            {
                PresetBookReference bookReference = context?.PreferredBook ?? new();
                references.Add(new() { Value = bookReference });
            }
            else if (e == typeof(LinkReference))
                references.Add(new() { Value = new LinkReference() });
        };
        rfLE.ClearItemsRequested += (s, e) => references.Clear();
        rfLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceBase> reference)
                references.Remove(reference);
        };
        rfLE.DuplicateItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceBase> x)
                references.Add(new() { Value = x.Value with { } });
        };
        rfLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceBase> reference)
            {
                int oldIndex = references.IndexOf(reference);
                int newIndex = Math.Max(oldIndex - 1, 0);
                references.Move(oldIndex, newIndex);
            }
        };
        rfLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceBase> reference)
            {
                int oldIndex = references.IndexOf(reference);
                int newIndex = Math.Min(oldIndex + 1, references.Count - 1);
                references.Move(oldIndex, newIndex);
            }
        };

        #endregion
    }

    #endregion
}
