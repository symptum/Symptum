using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.Core.Extensions;
using Symptum.Common.ProjectSystem;
using Uno.Extensions.Specialized;

namespace Symptum.Editor.Pages;

public sealed partial class ReferenceValueGroupEditorPage : EditorPageBase
{
    private ReferenceValueGroup? currentGroup;
    private ReferenceValueParameterEditorDialog? parameterEditorDialog;
    private ResourcePropertiesEditorDialog? propertyEditorDialog;
    private ConfirmationDialog? confirmationDialog;

    public ReferenceValueGroupEditorPage()
    {
        InitializeComponent();
        PageName = "Reference Value Group Editor";
        IconSource = DefaultIconSources.TableViewIconSource;
        Loaded += ReferenceValueGroupEditorPage_Loaded;
        Unloaded += ReferenceValueGroupEditorPage_Unloaded;
    }

    private void ReferenceValueGroupEditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        parameterEditorDialog = EditorPagesManager.CreateOrGetDialog<ReferenceValueParameterEditorDialog>();
        propertyEditorDialog = EditorPagesManager.CreateOrGetDialog<ResourcePropertiesEditorDialog>();
        SetupFindControl();
    }

    private void ReferenceValueGroupEditorPage_Unloaded(object sender, RoutedEventArgs e)
    {
        currentGroup = null;
        parameterEditorDialog = null;
        propertyEditorDialog = null;
        confirmationDialog = null;
    }

    protected override void OnSetEditableContent(IResource? resource)
    {
        if (resource is ReferenceValueGroup group)
            LoadGroup(group);
        else
            Reset();
    }

    private void Reset()
    {
        tableView.SelectedItems.Clear();
        tableView.ItemsSource = null;
        tableView.IsEnabled = false;
        saveButton.IsEnabled = false;
        addButton.IsEnabled = false;
        findButton.IsEnabled = false;
        currentGroup = null;
        SetCountsText(true);
    }

    private void LoadGroup(ReferenceValueGroup? group)
    {
        if (group == null) return;

        currentGroup = group;
        group.Parameters ??= [];
        tableView.ItemsSource = group.Parameters;
        tableView.IsEnabled = true;
        saveButton.IsEnabled = true;
        addButton.IsEnabled = true;
        findButton.IsEnabled = true;
        SetCountsText();
    }

    private void SetCountsText(bool clear = false)
    {
        if (clear)
            countTextBlock.Text = null;
        else
            countTextBlock.Text = $"{currentGroup?.Parameters?.Count} Parameters, {tableView.SelectedItems.Count} Selected";
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (currentGroup != null)
        {
            bool saved = await ProjectSystemManager.SaveResourceAndAncestorAsync(currentGroup);
            HasUnsavedChanges = !saved;
            if (saved)
                WriteToOutput($"Saved: {currentGroup.Title}");
        }
        _isBeingSaved = false;
    }

    private async void PropsButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentGroup != null && propertyEditorDialog != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(currentGroup);
            if (result == EditorResult.Update)
            {
                HasUnsavedChanges = true;
                WriteToOutput($"Updated properties: {currentGroup.Title}");
            }
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentGroup != null && parameterEditorDialog != null)
        {
            parameterEditorDialog.XamlRoot = XamlRoot;
            var result = await parameterEditorDialog.CreateAsync();
            if (result == EditorResult.Create && parameterEditorDialog.Parameter is ReferenceValueParameter parameter)
            {
                currentGroup?.Parameters?.Add(parameter);
                tableView.SelectedItem = parameter;
                HasUnsavedChanges = true;
                SetCountsText();
                WriteToOutput($"Added parameter: {parameter.Title}");
            }
        }
    }

    private void TableView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = tableView.SelectedItems.Count;
        deleteButton.IsEnabled = count > 0;
        duplicateButton.IsEnabled = count > 0;
        editButton.IsEnabled = count == 1;
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        SetCountsText();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e) => await EnterEditParameterAsync();

    private async void TableView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => await EnterEditParameterAsync();

    private async Task EnterEditParameterAsync()
    {
        if (tableView.SelectedItems.Count == 0) return;
        if (tableView.SelectedItems[0] is ReferenceValueParameter parameter &&
            parameterEditorDialog != null)
        {
            parameterEditorDialog.XamlRoot = XamlRoot;
            var result = await parameterEditorDialog.EditAsync(parameter);
            if (result == EditorResult.Update)
            {
                HasUnsavedChanges = true;
                WriteToOutput($"Edited parameter: {parameter.Title}");
            }
        }
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (tableView.SelectedItems.Count == 0
            || currentGroup == null || currentGroup.Parameters == null) return;
        List<ReferenceValueParameter> toDupe = [];

        foreach (var item in tableView.SelectedItems)
        {
            if (item is ReferenceValueParameter parameter && currentGroup.Parameters.Contains(parameter))
                toDupe.Add(parameter);
        }
        tableView.SelectedItems.Clear();
        toDupe.ForEach(x => currentGroup?.Parameters?.Add(x.Clone()));
        toDupe.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (tableView.SelectedItems.Count == 0
            || currentGroup?.Parameters == null) return;

        confirmationDialog ??= EditorPagesManager.CreateOrGetDialog<ConfirmationDialog>();
        confirmationDialog?.XamlRoot = XamlRoot;
        var result = await confirmationDialog?.ConfirmDeletionAsync("Parameter(s)");
        if (result == EditorResult.Delete)
        {
            List<ReferenceValueParameter> toDelete = [];

            foreach (var item in tableView.SelectedItems)
            {
                if (item is ReferenceValueParameter parameter && currentGroup.Parameters.Contains(parameter))
                    toDelete.Add(parameter);
            }
            tableView.SelectedItems.Clear();
            toDelete.ForEach(x => currentGroup?.Parameters?.Remove(x));
            WriteToOutput($"Deleted {toDelete.Count} parameter(s)");
            toDelete.Clear();
            HasUnsavedChanges = true;
            SetCountsText();
        }
    }

    #region Find

    private void SetupFindControl()
    {
        List<string> columns =
        [
            nameof(ReferenceValueParameter.Id),
            nameof(ReferenceValueParameter.Title),
        ];

        findControl.FindContexts = columns;
        findControl.SelectedContext = columns[0];
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        findControl.ShowFindControl();
    }

    private void FindControl_QueryCleared(object? sender, EventArgs e)
    {
        var selectedItem = tableView.SelectedItem;
        if (currentGroup != null)
            tableView.ItemsSource = currentGroup.Parameters;
        tableView.SelectedItem = selectedItem;
        findTextBlock.Text = string.Empty;
    }

    private void FindControl_QuerySubmitted(object? sender, FindControlQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentGroup != null)
        {
            var parameters = from parameter in currentGroup?.Parameters
                             where ReferenceValueParameterPropertyMatchValue(parameter, e)
                             select parameter;
            tableView.ItemsSource = parameters;
            findTextBlock.Text = $"Find results for '{e.QueryText}' in {e.Context}. Matching Parameters: {parameters.Count()}";
        }
    }

    private bool ReferenceValueParameterPropertyMatchValue(ReferenceValueParameter parameter, FindControlQuerySubmittedEventArgs e) => e.Context switch
    {
        nameof(ReferenceValueParameter.Id) =>
            parameter?.Id?.Contains(e.QueryText, e.MatchCase, e.MatchWholeWord),
        nameof(ReferenceValueParameter.Title) =>
            parameter?.Title?.Contains(e.QueryText, e.MatchCase, e.MatchWholeWord),
        _ => false
    } ?? false;

    #endregion

    private bool CanMoveUp() => tableView.SelectedItems.Count == 1 && tableView.SelectedIndex != 0;

    private bool CanMoveDown() => tableView.SelectedItems.Count == 1 && tableView.SelectedIndex != currentGroup?.Parameters?.Count - 1;

    private void MoveParameter(int oldIndex, int newIndex)
    {
        currentGroup?.Parameters?.Move(oldIndex, newIndex);
        tableView.SelectedItems.Clear();
        tableView.SelectedItem = null;
        tableView.SelectedIndex = newIndex;
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        HasUnsavedChanges = true;
        WriteToOutput($"Moved parameter: {currentGroup?.Parameters?[newIndex]?.Title}");
        tableView.ScrollIntoView(tableView.SelectedItem, ScrollIntoViewAlignment.Default);
    }

    private void MoveParameterUp(bool toTop)
    {
        if (CanMoveUp())
        {
            int oldIndex = tableView.SelectedIndex;
            int newIndex = toTop ? 0 : Math.Max(tableView.SelectedIndex - 1, 0);
            MoveParameter(oldIndex, newIndex);
        }
    }

    private void MoveParameterDown(bool toBottom)
    {
        if (CanMoveDown())
        {
            int oldIndex = tableView.SelectedIndex;
            int last = currentGroup?.Parameters?.Count - 1 ?? 0;
            int newIndex = toBottom ? last : Math.Min(tableView.SelectedIndex + 1, last);
            MoveParameter(oldIndex, newIndex);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e) => MoveParameterUp(false);

    private void MoveToTopButton_Click(object sender, RoutedEventArgs e) => MoveParameterUp(true);

    private void MoveDownButton_Click(object sender, RoutedEventArgs e) => MoveParameterDown(false);

    private void MoveToBottomButton_Click(object sender, RoutedEventArgs e) => MoveParameterDown(true);
}
