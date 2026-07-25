using System.Collections.ObjectModel;
using Symptum.Core.Data.ReferenceValues;

namespace Symptum.Editor.Controls;

public sealed partial class ReferenceValueParameterEditorDialog : ContentDialog, IEditorDialog
{
    public ReferenceValueParameter? Parameter { get; private set; }

    public EditorResult EditResult { get; private set; } = EditorResult.None;

    private readonly ObservableCollection<ListEditorItemWrapper<ReferenceValueEntry>> entries = [];

    public ReferenceValueParameterEditorDialog()
    {
        InitializeComponent();
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
        enLE.ItemsSource = entries;
        enLE.ActionRequested += LE_ActionRequested;
    }

    private void ReferenceValueParameterEditor_Closing(ContentDialog sender, ContentDialogClosingEventArgs e)
    {
        enLE.ItemsSource = null;
        entries.ClearWrapperListSafe();
        enLE.ActionRequested -= LE_ActionRequested;
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

    private void LE_ActionRequested(object? s, ListEditorItemActionRequestedEventArgs e) =>
        ListEditorControl.HandleActionRequired(entries, e, () => new(), e => e.Clone());
}
