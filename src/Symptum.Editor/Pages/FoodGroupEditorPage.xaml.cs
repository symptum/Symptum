using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.Core.Data.Nutrition;
using Symptum.Core.Extensions;
using Symptum.Common.ProjectSystem;

namespace Symptum.Editor.Pages;

public sealed partial class FoodGroupEditorPage : EditorPageBase
{
    private FoodGroup? currentGroup;
    private FoodEditorDialog foodEditorDialog = new();
    private ResourcePropertiesEditorDialog propertyEditorDialog = new();

    private DeleteItemsDialog deleteEntriesDialog = new()
    {
        Title = "Delete Food(s)?",
        Content = "Do you want to delete the food(s)?\nOnce you delete you won't be able to restore."
    };

    private bool _isFiltered = false;

    public FoodGroupEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.TableViewIconSource;
        SetupFindControl();
    }

    protected override void OnSetEditableContent(IResource? resource)
    {
        if (resource is FoodGroup group)
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

    private void LoadGroup(FoodGroup? group)
    {
        if (group == null) return;

        currentGroup = group;
        group.Foods ??= [];
        tableView.ItemsSource = group.Foods;
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
            countTextBlock.Text = $"{currentGroup?.Foods?.Count} Foods, {tableView.SelectedItems.Count} Selected";
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved) return;

        _isBeingSaved = true;

        if (currentGroup != null)
            HasUnsavedChanges = !await ProjectSystemManager.SaveResourceAndAncestorAsync(currentGroup);
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
            foodEditorDialog.XamlRoot = XamlRoot;
            var result = await foodEditorDialog.CreateAsync();
            if (result == EditorResult.Create && foodEditorDialog.Food is Food food)
            {
                currentGroup?.Foods?.Add(food);
                tableView.SelectedItem = food;
                HasUnsavedChanges = true;
                SetCountsText();
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

    private async void EditButton_Click(object sender, RoutedEventArgs e) => await EnterEditFoodAsync();

    private async void TableView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => await EnterEditFoodAsync();

    private async Task EnterEditFoodAsync()
    {
        if (tableView.SelectedItems.Count == 0) return;
        if (tableView.SelectedItems[0] is Food food)
        {
            foodEditorDialog.XamlRoot = XamlRoot;
            var result = await foodEditorDialog.EditAsync(food);
            if (result == EditorResult.Update || result == EditorResult.Save)
                HasUnsavedChanges = true;
        }
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (tableView.SelectedItems.Count == 0
            || currentGroup?.Foods == null) return;
        List<Food> toDupe = [];

        foreach (var item in tableView.SelectedItems)
        {
            if (item is Food food && currentGroup.Foods.Contains(food))
                toDupe.Add(food);
        }
        tableView.SelectedItems.Clear();
        //toDupe.ForEach(x => currentGroup?.Foods?.Add(x.Clone()));
        toDupe.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (tableView.SelectedItems.Count == 0
            || currentGroup?.Foods == null) return;

        deleteEntriesDialog.XamlRoot = XamlRoot;
        var result = await deleteEntriesDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            List<Food> toDelete = [];

            foreach (var item in tableView.SelectedItems)
            {
                if (item is Food food && currentGroup.Foods.Contains(food))
                    toDelete.Add(food);
            }
            tableView.SelectedItems.Clear();
            toDelete.ForEach(x => currentGroup?.Foods?.Remove(x));
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
            nameof(Food.Title),
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
            tableView.ItemsSource = currentGroup.Foods;
        tableView.SelectedItem = selectedItem;
        findTextBlock.Text = string.Empty;
        OnFilter(false);
    }

    private void FindControl_QuerySubmitted(object? sender, FindControlQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentGroup != null)
        {
            var foods = new ObservableCollection<Food>(from food in currentGroup?.Foods?.ToList()
                                                       where FoodPropertyMatchValue(food, e)
                                                       select food);
            tableView.ItemsSource = foods;
            findTextBlock.Text = $"Find results for '{e.QueryText}' in {e.Context}. Matching Foods: {foods.Count}";
            OnFilter(true);
        }
    }

    private void OnFilter(bool filtered) => _isFiltered = filtered;

    // TODO: Implement Match Whole Word
    private bool FoodPropertyMatchValue(Food food, FindControlQuerySubmittedEventArgs e) => e.Context switch
    {
        nameof(Food.Title) =>
            food?.Title?.Contains(e.QueryText, e.MatchCase, e.MatchWholeWord),
        _ => false
    } ?? false;

    #endregion

    private bool CanMoveUp() => tableView.SelectedItems.Count == 1 && tableView.SelectedIndex != 0;

    private bool CanMoveDown() => tableView.SelectedItems.Count == 1 && tableView.SelectedIndex != currentGroup?.Foods?.Count - 1;

    private void MoveFood(int oldIndex, int newIndex)
    {
        currentGroup?.Foods?.Move(oldIndex, newIndex);
        tableView.SelectedItems.Clear();
        tableView.SelectedItem = null;
        tableView.SelectedIndex = newIndex;
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        HasUnsavedChanges = true;
        tableView.ScrollIntoView(tableView.SelectedItem, ScrollIntoViewAlignment.Default);
    }

    private void MoveFoodUp(bool toTop)
    {
        if (CanMoveUp())
        {
            int oldIndex = tableView.SelectedIndex;
            int newIndex = toTop ? 0 : Math.Max(tableView.SelectedIndex - 1, 0);
            MoveFood(oldIndex, newIndex);
        }
    }

    private void MoveFoodDown(bool toBottom)
    {
        if (CanMoveDown())
        {
            int oldIndex = tableView.SelectedIndex;
            int last = currentGroup?.Foods?.Count - 1 ?? 0;
            int newIndex = toBottom ? last : Math.Min(tableView.SelectedIndex + 1, last);
            MoveFood(oldIndex, newIndex);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e) => MoveFoodUp(false);

    private void MoveToTopButton_Click(object sender, RoutedEventArgs e) => MoveFoodUp(true);

    private void MoveDownButton_Click(object sender, RoutedEventArgs e) => MoveFoodDown(false);

    private void MoveToBottomButton_Click(object sender, RoutedEventArgs e) => MoveFoodDown(true);
}
