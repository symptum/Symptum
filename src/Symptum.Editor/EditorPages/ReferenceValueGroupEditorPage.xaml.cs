using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Common.Helpers;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;

namespace Symptum.Editor.EditorPages;

public sealed partial class ReferenceValueGroupEditorPage : EditorPageBase
{
    private ReferenceValueGroup? currentGroup;
    private FindFlyout? findFlyout;
    private ReferenceValueParameterEditorDialog parameterEditorDialog = new();
    private ResourcePropertiesEditorDialog propertyEditorDialog = new();

    private DeleteItemsDialog deleteEntriesDialog = new()
    {
        Title = "Delete Parameter(s)?",
        Content = "Do you want to delete the parameter(s)?\nOnce you delete you won't be able to restore."
    };

    private bool _isFiltered = false;

    public ReferenceValueGroupEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.DataGridIconSource;
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
        dataGrid.SelectedItems.Clear();
        dataGrid.ItemsSource = null;
        dataGrid.IsEnabled = false;
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
        dataGrid.ItemsSource = group.Parameters;
        dataGrid.IsEnabled = true;
        saveButton.IsEnabled = true;
        addButton.IsEnabled = true;
        findButton.IsEnabled = true;
        SetCountsText();

        var binding = new Binding { Path = new PropertyPath(nameof(Title)), Source = group };
        SetBinding(TitleProperty, binding);
    }

    private void SetCountsText(bool clear = false)
    {
        if (clear)
            countTextBlock.Text = null;
        else
            countTextBlock.Text = $"{currentGroup?.Parameters?.Count} Parameters, {dataGrid.SelectedItems.Count} Selected";
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (currentGroup != null)
            HasUnsavedChanges = !await ResourceHelper.SaveResourceAsync(currentGroup);
        _isBeingSaved = false;
    }

    private async void PropsButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentGroup != null)
        {
            propertyEditorDialog.XamlRoot = XamlRoot;
            var result = await propertyEditorDialog.EditAsync(currentGroup);
            if (result == EditorResult.Update)
                HasUnsavedChanges = true;
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentGroup != null)
        {
            parameterEditorDialog.XamlRoot = XamlRoot;
            var result = await parameterEditorDialog.CreateAsync();
            if (result == EditorResult.Create && parameterEditorDialog.Parameter is ReferenceValueParameter parameter)
            {
                currentGroup?.Parameters?.Add(parameter);
                dataGrid.SelectedItem = parameter;
                HasUnsavedChanges = true;
                SetCountsText();
            }
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = dataGrid.SelectedItems.Count;
        deleteButton.IsEnabled = count > 0;
        duplicateButton.IsEnabled = count > 0;
        editButton.IsEnabled = count == 1;
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        SetCountsText();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        await EnterEditParameterAsync();
    }

    private async void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await EnterEditParameterAsync();
    }

    private async Task EnterEditParameterAsync()
    {
        if (dataGrid.SelectedItems.Count == 0) return;
        if (dataGrid.SelectedItems[0] is ReferenceValueParameter parameter)
        {
            parameterEditorDialog.XamlRoot = XamlRoot;
            var result = await parameterEditorDialog.EditAsync(parameter);
            if (result == EditorResult.Update || result == EditorResult.Save)
                HasUnsavedChanges = true;
        }
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0
            || currentGroup == null || currentGroup.Parameters == null) return;
        List<ReferenceValueParameter> toDupe = [];

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is ReferenceValueParameter parameter && currentGroup.Parameters.Contains(parameter))
                toDupe.Add(parameter);
        }
        dataGrid.SelectedItems.Clear();
        //toDupe.ForEach(x => currentGroup?.Parameters?.Add(x.Clone()));
        toDupe.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0
            || currentGroup?.Parameters == null) return;

        deleteEntriesDialog.XamlRoot = XamlRoot;
        var result = await deleteEntriesDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            List<ReferenceValueParameter> toDelete = [];

            foreach (var item in dataGrid.SelectedItems)
            {
                if (item is ReferenceValueParameter parameter && currentGroup.Parameters.Contains(parameter))
                    toDelete.Add(parameter);
            }
            dataGrid.SelectedItems.Clear();
            toDelete.ForEach(x => currentGroup?.Parameters?.Remove(x));
            toDelete.Clear();
            HasUnsavedChanges = true;
            SetCountsText();
        }
    }

    private void FindButton_Click(object sender, RoutedEventArgs e)
    {
        if (findFlyout == null)
        {
            List<string> columns =
            [
                nameof(ReferenceValueParameter.Title),
            ];

            findFlyout = new()
            {
                FindContexts = columns,
                SelectedContext = columns[0],
            };

            findFlyout.QuerySubmitted += FindFlyout_QuerySubmitted;
            findFlyout.QueryCleared += FindFlyout_QueryCleared;
        }

        findFlyout.XamlRoot = XamlRoot;

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        FlyoutShowOptions flyoutShowOptions = new()
        {
            Position = new(ActualWidth, 150),
            Placement = FlyoutPlacementMode.Bottom
        };
        findFlyout.ShowAt(showOptions: flyoutShowOptions);
#else
        findFlyout.ShowAt(findButton, new() { Placement = FlyoutPlacementMode.Bottom });
#endif
    }

    private void FindFlyout_QueryCleared(object? sender, EventArgs e)
    {
        var selectedItem = dataGrid.SelectedItem;
        if (currentGroup != null)
            dataGrid.ItemsSource = currentGroup.Parameters;
        dataGrid.SelectedItem = selectedItem;
        findTextBlock.Text = string.Empty;
        OnFilter(false);
    }

    private void FindFlyout_QuerySubmitted(object? sender, FindFlyoutQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentGroup != null)
        {
            var parameters = new ObservableCollection<ReferenceValueParameter>(from parameter in currentGroup?.Parameters?.ToList()
                                                                               where ReferenceValueParameterPropertyMatchValue(parameter, e)
                                                                               select parameter);
            dataGrid.ItemsSource = parameters;
            findTextBlock.Text = $"Find results for '{e.QueryText}' in {e.Context}. Matching Parameters: {parameters.Count}";
            OnFilter(true);
        }
    }

    private void OnFilter(bool filtered)
    {
        _isFiltered = filtered;
    }

    // TODO: Implement Match Whole Word
    private bool ReferenceValueParameterPropertyMatchValue(ReferenceValueParameter parameter, FindFlyoutQuerySubmittedEventArgs e)
    {
        return e.Context switch
        {
            nameof(ReferenceValueParameter.Title) =>
                parameter?.Title?.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
            _ => false
        } ?? false;
    }

    private bool CanMoveUp() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != 0;

    private bool CanMoveDown() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != currentGroup?.Parameters?.Count - 1;

    private void MoveParameter(int oldIndex, int newIndex)
    {
        currentGroup?.Parameters?.Move(oldIndex, newIndex);
        dataGrid.SelectedItems.Clear();
        dataGrid.SelectedItem = null;
        dataGrid.SelectedIndex = newIndex;
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        HasUnsavedChanges = true;
        dataGrid.ScrollIntoView(dataGrid.SelectedItem, null);
    }

    private void MoveParameterUp(bool toTop)
    {
        if (CanMoveUp())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int newIndex = toTop ? 0 : Math.Max(dataGrid.SelectedIndex - 1, 0);
            MoveParameter(oldIndex, newIndex);
        }
    }

    private void MoveParameterDown(bool toBottom)
    {
        if (CanMoveDown())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int last = currentGroup?.Parameters?.Count - 1 ?? 0;
            int newIndex = toBottom ? last : Math.Min(dataGrid.SelectedIndex + 1, last);
            MoveParameter(oldIndex, newIndex);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveParameterUp(false);
    }

    private void MoveToTopButton_Click(object sender, RoutedEventArgs e)
    {
        MoveParameterUp(true);
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveParameterDown(false);
    }

    private void MoveToBottomButton_Click(object sender, RoutedEventArgs e)
    {
        MoveParameterDown(true);
    }

    private void DataGrid_LoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }
}
