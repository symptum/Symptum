using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Core.TypeConversion;
using Symptum.Common.Helpers;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;

namespace Symptum.Editor.EditorPages;

public sealed partial class QuestionTopicEditorPage : Page, IEditorPage
{
    private QuestionBankTopic? currentTopic;
    private FindFlyout? findFlyout;
    private QuestionEditorDialog questionEditorDialog = new();

    private DeleteItemsDialog deleteEntriesDialog = new()
    {
        Title = "Delete Question(s)?",
        Content = "Do you want to delete the question(s)?\nOnce you delete you won't be able to restore."
    };

    private bool _isFiltered = false;

    public QuestionTopicEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.DataGridIconSource;
    }

    #region Properties

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(QuestionTopicEditorPage),
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
            typeof(QuestionTopicEditorPage),
            new PropertyMetadata(null, OnEditableContentChanged));

    private static void OnEditableContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuestionTopicEditorPage questionTopicEditorPage)
        {
            questionTopicEditorPage.SetEditableContent(e.NewValue as IResource);
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
        typeof(QuestionTopicEditorPage),
        new PropertyMetadata(false));

    public bool HasUnsavedChanges
    {
        get => (bool)GetValue(HasUnsavedChangesProperty);
        set => SetValue(HasUnsavedChangesProperty, value);
    }

    #endregion

    private void SetEditableContent(IResource? resource)
    {
        if (resource is QuestionBankTopic topic)
            LoadTopic(topic);
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
        currentTopic = null;
        SetCountsText(true);
        DataContext = null;
    }

    private void LoadTopic(QuestionBankTopic? topic)
    {
        if (topic == null) return;

        currentTopic = topic;
        topic.Entries ??= [];
        dataGrid.ItemsSource = topic.Entries;
        dataGrid.IsEnabled = true;
        saveButton.IsEnabled = true;
        addButton.IsEnabled = true;
        findButton.IsEnabled = true;
        SetCountsText();

        DataContext = currentTopic;

        var binding = new Binding { Path = new PropertyPath(nameof(Title)) };
        SetBinding(TitleProperty, binding);
    }

    private void SetCountsText(bool clear = false)
    {
        if (clear)
            countTextBlock.Text = null;
        else
            countTextBlock.Text = $"{currentTopic?.Entries?.Count} Entries, {dataGrid.SelectedItems.Count} Selected";
    }

    private bool _isBeingSaved = false;

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved)
        {
            return;
        }

        _isBeingSaved = true;

        if (currentTopic != null)
            HasUnsavedChanges = !await ResourceHelper.SaveCSVFileAsync(currentTopic);
        _isBeingSaved = false;
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentTopic != null)
        {
            questionEditorDialog.XamlRoot = XamlRoot;
            var result = await questionEditorDialog.CreateAsync();
            if (result == EditorResult.Create && questionEditorDialog.QuestionEntry is QuestionEntry entry)
            {
                currentTopic?.Entries?.Add(entry);
                dataGrid.SelectedItem = entry;
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
        await EnterEditQuestionAsync();
    }

    private async void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await EnterEditQuestionAsync();
    }

    private async Task EnterEditQuestionAsync()
    {
        if (dataGrid.SelectedItems.Count == 0) return;
        if (dataGrid.SelectedItems[0] is QuestionEntry entry)
        {
            questionEditorDialog.XamlRoot = XamlRoot;
            var result = await questionEditorDialog.EditAsync(entry);
            if (result == EditorResult.Update || result == EditorResult.Save)
            {
                HasUnsavedChanges = true;
            }
        }
    }

    private void DuplicateButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0
            || currentTopic?.Entries == null) return;
        List<QuestionEntry> toDupe = [];

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is QuestionEntry entry && currentTopic.Entries.Contains(entry))
                toDupe.Add(entry);
        }
        dataGrid.SelectedItems.Clear();
        toDupe.ForEach(x => currentTopic?.Entries?.Add(x.Clone()));
        toDupe.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0
            || currentTopic?.Entries == null) return;

        deleteEntriesDialog.XamlRoot = XamlRoot;
        var result = await deleteEntriesDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            List<QuestionEntry> toDelete = [];

            foreach (var item in dataGrid.SelectedItems)
            {
                if (item is QuestionEntry entry && currentTopic.Entries.Contains(entry))
                    toDelete.Add(entry);
            }
            dataGrid.SelectedItems.Clear();
            toDelete.ForEach(x => currentTopic?.Entries?.Remove(x));
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
                nameof(QuestionEntry.Title),
                nameof(QuestionEntry.Descriptions),
                nameof(QuestionEntry.YearsAsked),
                nameof(QuestionEntry.ProbableCases)
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
        if (currentTopic != null)
            dataGrid.ItemsSource = currentTopic.Entries;
        dataGrid.SelectedItem = selectedItem;
        findTextBlock.Text = string.Empty;
        OnFilter(false);
    }

    private void FindFlyout_QuerySubmitted(object? sender, FindFlyoutQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentTopic != null)
        {
            var entries = new ObservableCollection<QuestionEntry>(from question in currentTopic?.Entries?.ToList()
                                                                  where QuestionEntryPropertyMatchValue(question, e)
                                                                  select question);
            dataGrid.ItemsSource = entries;
            findTextBlock.Text = $"Find results for '{e.QueryText}' in {e.Context}. Matching entries: {entries.Count}";
            OnFilter(true);
        }
    }

    private void OnFilter(bool filtered)
    {
        _isFiltered = filtered;
    }

    // TODO: Implement Match Whole Word
    private bool QuestionEntryPropertyMatchValue(QuestionEntry question, FindFlyoutQuerySubmittedEventArgs e)
    {
        return e.Context switch
        {
            nameof(QuestionEntry.Title) =>
                question?.Title?.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
            nameof(QuestionEntry.Descriptions) =>
                ListToStringConversion.ConvertToString<string>(question?.Descriptions, x => x)?
                    .Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
            nameof(QuestionEntry.YearsAsked) =>
                ListToStringConversion.ConvertToString<DateOnly>(question?.YearsAsked, ListToStringConversion.ElementToStringForDateOnly)?
                    .Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
            nameof(QuestionEntry.ProbableCases) =>
                ListToStringConversion.ConvertToString<string>(question?.ProbableCases, x => x)?
                    .Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase),
            _ => false,
        } ?? false;
    }

    private void SortButton_Click(object sender, RoutedEventArgs e)
    {
        //SortEntries();
    }

    private void SortYearsButton_Click(object sender, RoutedEventArgs e)
    {
        SortYearsAsked();
    }

    //private void SortEntries()
    //{
    //    var x = currentTopic?.QuestionEntries?.Order().ToList();
    //    StringBuilder mdBuilder = new();
    //    MarkdownHelper.GenerateMarkdownForQuestionEntries(x, ref mdBuilder);
    //    System.Diagnostics.Debug.WriteLine(mdBuilder.ToString());
    //}

    private void SortYearsAsked()
    {
        if (currentTopic?.Entries == null) return;

        foreach (var entry in currentTopic.Entries)
        {
            entry?.YearsAsked?.Sort(new Comparison<DateOnly>((d1, d2) => d2.CompareTo(d1)));
        }
        HasUnsavedChanges = true;
    }

    private bool CanMoveUp() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != 0;

    private bool CanMoveDown() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != currentTopic?.Entries?.Count - 1;

    private void MoveEntry(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex) return;
        currentTopic?.Entries?.Move(oldIndex, newIndex);
        dataGrid.SelectedItems.Clear();
        dataGrid.SelectedItem = null;
        dataGrid.SelectedIndex = newIndex;
        moveUpButton.IsEnabled = moveToTopButton.IsEnabled = CanMoveUp();
        moveDownButton.IsEnabled = moveToBottomButton.IsEnabled = CanMoveDown();
        HasUnsavedChanges = true;
        dataGrid.ScrollIntoView(dataGrid.SelectedItem, null);
    }

    private void MoveEntryUp(bool toTop)
    {
        if (CanMoveUp())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int newIndex = toTop ? 0 : Math.Max(dataGrid.SelectedIndex - 1, 0);
            MoveEntry(oldIndex, newIndex);
        }
    }

    private void MoveEntryDown(bool toBottom)
    {
        if (CanMoveDown())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int last = currentTopic?.Entries?.Count - 1 ?? 0;
            int newIndex = toBottom ? last : Math.Min(dataGrid.SelectedIndex + 1, last);
            MoveEntry(oldIndex, newIndex);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveEntryUp(false);
    }

    private void MoveToTopButton_Click(object sender, RoutedEventArgs e)
    {
        MoveEntryUp(true);
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveEntryDown(false);
    }

    private void MoveToBottomButton_Click(object sender, RoutedEventArgs e)
    {
        MoveEntryDown(true);
    }

    private void DataGrid_LoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }

    private void ReorderButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0) return;
        if (dataGrid.SelectedItems[0] is QuestionEntry currentEntry
            && currentEntry.YearsAsked?.Count > 0)
        {
            var entries = currentTopic?.Entries;
            if (entries != null)
            {
                // Finds the first entry which has a year greater than or equal to the selected one
                QuestionEntry? neighbor = null;
                foreach (var entry in entries)
                {
                    if (entry == currentEntry) continue;
                    if (entry.YearsAsked?.Count > 0
                        && entry.YearsAsked[0] >= currentEntry.YearsAsked[0])
                        neighbor = entry;
                    else break;
                }

                int oldIndex = entries.IndexOf(currentEntry);
                int newIndex = neighbor != null ? Math.Min(entries.IndexOf(neighbor) + 1, entries.Count - 1) : 0; // Puts it next to it
                // If we move from up to down, the index will be +1 due to the current entry being included in the neighbor's index
                int dI = oldIndex < newIndex ? -1 : 0;
                MoveEntry(oldIndex, newIndex + dI);
            }
        }
    }
}
