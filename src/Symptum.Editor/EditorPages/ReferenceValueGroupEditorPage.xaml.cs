using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.Editor.Helpers;

namespace Symptum.Editor.EditorPages;

public sealed partial class ReferenceValueGroupEditorPage : Page, IEditorPage
{
    private ReferenceValueGroup? currentGroup;
    private FindFlyout? findFlyout;
    private bool _isFiltered = false;

    public ReferenceValueGroupEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.QuestionBankTopicIconSource;
    }

    #region Properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ReferenceValueGroupEditorPage),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IconSource IconSource { get; private set; }

    public static readonly DependencyProperty EditableContentProperty =
        DependencyProperty.Register(
            nameof(EditableContent),
            typeof(IResource),
            typeof(ReferenceValueGroupEditorPage),
            new PropertyMetadata(null, OnEditableContentChanged));

    private static void OnEditableContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ReferenceValueGroupEditorPage referenceValueGroupEditorPage)
        {
            referenceValueGroupEditorPage.SetEditableContent(e.NewValue as IResource);
        }
    }

    public IResource? EditableContent
    {
        get => (IResource?)GetValue(EditableContentProperty);
        set => SetValue(EditableContentProperty, value);
    }

    public static readonly DependencyProperty HasUnsavedChangesProperty = DependencyProperty.Register(
        nameof(HasUnsavedChanges),
        typeof(bool),
        typeof(ReferenceValueGroupEditorPage),
        new PropertyMetadata(false));

    public bool HasUnsavedChanges
    {
        get => (bool)GetValue(HasUnsavedChangesProperty);
        set => SetValue(HasUnsavedChangesProperty, value);
    }

    #endregion

    private void SetEditableContent(IResource? resource)
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
        DataContext = null;
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

        DataContext = group;

        var binding = new Binding { Path = new PropertyPath(nameof(Title)) };
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
        if (_isBeingSaved)
        {
            return;
        }

        _isBeingSaved = true;

        bool pathExists = await ResourceHelper.VerifyWorkPathAsync();
        if (pathExists && currentGroup != null)
            HasUnsavedChanges = !await ResourceHelper.SaveCSVFileAsync(currentGroup);
        _isBeingSaved = false;
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentGroup != null)
        {
            ReferenceValueEntry entry = new() { Title = "entry" + currentGroup?.Parameters?.Count, Data = [new() { Values = "[13,16]", Unit = "g/dL" }], Inference = "Normal", Remarks = "Normal range" };
            currentGroup?.Parameters?.Add(new("test" + currentGroup?.Parameters?.Count) { Entries = [entry] });
            //questionEditorDialog.XamlRoot = XamlRoot;
            //var result = await questionEditorDialog.CreateAsync();
            //if (result == EditorResult.Create)
            //{
            //    currentTopic?.Entries?.Add(questionEditorDialog.QuestionEntry);
            //    HasUnsavedChanges = true;
            //    SetCountsText();
            //}
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = dataGrid.SelectedItems.Count;
        deleteButton.IsEnabled = count > 0;
        duplicateButton.IsEnabled = count > 0;
        editButton.IsEnabled = count == 1;
        moveDownButton.IsEnabled = CanMoveDown();
        moveUpButton.IsEnabled = CanMoveUp();
        SetCountsText();
    }

    private void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentGroup == null) return;
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

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentGroup == null) return;
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
        if (currentGroup != null)
            dataGrid.ItemsSource = currentGroup.Parameters;
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
        switch (e.Context)
        {
            case nameof(ReferenceValueParameter.Title):
                {
                    return parameter.Title.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };

            default: return false;
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (CanMoveUp())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int newIndex = Math.Max(dataGrid.SelectedIndex - 1, 0);
            currentGroup?.Parameters?.Move(oldIndex, newIndex);
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (CanMoveDown())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int count = currentGroup?.Parameters?.Count - 1 ?? 0;
            int newIndex = Math.Min(dataGrid.SelectedIndex + 1, count);
            currentGroup?.Parameters?.Move(oldIndex, newIndex);
        }
    }

    private bool CanMoveUp() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != 0;

    private bool CanMoveDown() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != currentGroup?.Parameters?.Count - 1;
}
