using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor.Controls;

public sealed partial class ReferenceValueParameterEditorDialog : ContentDialog
{
    public ReferenceValueParameter? Parameter { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private readonly ObservableCollection<ListEditorItemWrapper<ReferenceValueEntry>> entries = [];

    public ReferenceValueParameterEditorDialog()
    {
        this.InitializeComponent();
        Opened += ReferenceValueParameterEditor_Opened;
        PrimaryButtonClick += ReferenceValueParameterEditor_PrimaryButtonClick;
        CloseButtonClick += ReferenceValueParameterEditor_CloseButtonClick;

        HandleListEditors();
    }

    private void ReferenceValueParameterEditor_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = EditorResult.Cancel;
        ClearParameter();
    }

    private void ReferenceValueParameterEditor_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EditResult = _isCreate ? EditorResult.Create : EditorResult.Update;
        UpdateParameter();
        ClearParameter();
    }

    private void ReferenceValueParameterEditor_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        LoadParameter();
    }

    private bool _isCreate = false;

    public async Task<EditorResult> CreateAsync()
    {
        Title = "Add a New Parameter";
        PrimaryButtonText = "Add";
        Parameter = null;
        _isCreate = true;
        await ShowAsync();
        return EditResult;
    }

    public async Task<EditorResult> EditAsync(ReferenceValueParameter parameter)
    {
        Title = "Edit Parameter";
        PrimaryButtonText = "Update";
        Parameter = parameter;
        _isCreate = false;
        await ShowAsync();
        return EditResult;
    }

    private void LoadParameter()
    {
        if (Parameter == null) return;

        titleTB.Text = Parameter.Title;
        entries.LoadFromList(Parameter.Entries);
    }

    private void UpdateParameter()
    {
        Parameter ??= new();
        Parameter.Title = titleTB.Text;
        Parameter.Entries = entries.UnwrapToList();
    }

    private void ClearParameter()
    {
        titleTB.Text = string.Empty;
        entries.Clear();
    }

    private void HandleListEditors()
    {
        enLE.ItemsSource = entries;
        enLE.AddItemRequested += (s, e) => entries.Add(new ListEditorItemWrapper<ReferenceValueEntry>(new()));
        enLE.ClearItemsRequested += (s, e) => entries.Clear();
        enLE.RemoveItemRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueEntry> entry)
                entries.Remove(entry);
        };
        enLE.DuplicateItemRequested += (s, e) =>
        {
            //if (e is ListEditorItemWrapper<ReferenceValueEntry> entry)
            //    entries.Add(new() { Value = entry.Value });
        };
        enLE.MoveItemUpRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueEntry> entry)
            {
                int oldIndex = entries.IndexOf(entry);
                int newIndex = Math.Max(oldIndex - 1, 0);
                entries.Move(oldIndex, newIndex);
            }
        };
        enLE.MoveItemDownRequested += (s, e) =>
        {
            if (e is ListEditorItemWrapper<ReferenceValueEntry> entry)
            {
                int oldIndex = entries.IndexOf(entry);
                int newIndex = Math.Min(oldIndex + 1, entries.Count - 1);
                entries.Move(oldIndex, newIndex);
            }
        };
    }
}
