using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Symptum.Core.Management.Resources;
using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Core.TypeConversion;
using Symptum.Editor.Common;
using Symptum.Editor.Controls;
using Symptum.Editor.Helpers;

namespace Symptum.Editor.EditorPages;

public sealed partial class QuestionTopicEditorPage : Page, IEditorPage
{
    private QuestionBankTopic? currentTopic;
    private FindFlyout? findFlyout;
    private QuestionEditorDialog questionEditorDialog = new();
    private bool _isFiltered = false;

    public QuestionTopicEditorPage()
    {
        InitializeComponent();
        IconSource = DefaultIconSources.QuestionBankTopicIconSource;
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

    private void SetEditableContent(IResource? topic)
    {
        if (topic == null)
            Reset();
        else
            LoadTopic(topic as QuestionBankTopic);
    }

    private void Reset()
    {
        dataGrid.SelectedItems.Clear();
        dataGrid.ItemsSource = null;
        dataGrid.IsEnabled = false;
        saveTopicButton.IsEnabled = false;
        addQuestionButton.IsEnabled = false;
        findQuestionButton.IsEnabled = false;
        currentTopic = null;
        SetCountsText(true);
        DataContext = null;
    }

    private void LoadTopic(QuestionBankTopic? topic)
    {
        if (topic == null) return;

        currentTopic = topic;
        topic.QuestionEntries ??= [];
        dataGrid.ItemsSource = topic.QuestionEntries;
        dataGrid.IsEnabled = true;
        saveTopicButton.IsEnabled = true;
        addQuestionButton.IsEnabled = true;
        findQuestionButton.IsEnabled = true;
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
            countTextBlock.Text = $"{currentTopic?.QuestionEntries?.Count} Entries, {dataGrid.SelectedItems.Count} Selected";
    }

    private bool _isBeingSaved = false;

    private async void SaveTopicButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBeingSaved)
        {
            return;
        }

        _isBeingSaved = true;

        bool pathExists = await ResourceHelper.VerifyWorkPathAsync();
        if (pathExists && currentTopic != null)
            HasUnsavedChanges = !await ResourceHelper.SaveQuestionBankTopicAsync(currentTopic);
        _isBeingSaved = false;
    }

    private async void AddQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (currentTopic != null)
        {
            questionEditorDialog.XamlRoot = XamlRoot;
            var result = await questionEditorDialog.CreateAsync();
            if (result == EditorResult.Create)
            {
                currentTopic?.QuestionEntries?.Add(questionEditorDialog.QuestionEntry);
                HasUnsavedChanges = true;
                SetCountsText();
            }
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int count = dataGrid.SelectedItems.Count;
        deleteQuestionsButton.IsEnabled = count > 0;
        duplicateQuestionButton.IsEnabled = count > 0;
        editQuestionButton.IsEnabled = count == 1;
        moveQuestionDownButton.IsEnabled = CanMoveDown();
        moveQuestionUpButton.IsEnabled = CanMoveUp();
        SetCountsText();
    }

    private async void EditQuestionButton_Click(object sender, RoutedEventArgs e)
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

    private void DuplicateQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentTopic == null) return;
        List<QuestionEntry> toDupe = [];

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is QuestionEntry entry && currentTopic.QuestionEntries.Contains(entry))
                toDupe.Add(entry);
        }
        dataGrid.SelectedItems.Clear();
        toDupe.ForEach(x => currentTopic?.QuestionEntries?.Add(x.Clone()));
        toDupe.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private void DeleteQuestionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrid.SelectedItems.Count == 0 || currentTopic == null) return;
        List<QuestionEntry> toDelete = [];

        foreach (var item in dataGrid.SelectedItems)
        {
            if (item is QuestionEntry entry && currentTopic.QuestionEntries.Contains(entry))
                toDelete.Add(entry);
        }
        dataGrid.SelectedItems.Clear();
        toDelete.ForEach(x => currentTopic?.QuestionEntries?.Remove(x));
        toDelete.Clear();
        HasUnsavedChanges = true;
        SetCountsText();
    }

    private void FindQuestionButton_Click(object sender, RoutedEventArgs e)
    {
        if (findFlyout == null)
        {
            List<string> columns =
            [
                nameof(QuestionEntry.Title),
                nameof(QuestionEntry.Descriptions),
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
        findFlyout.ShowAt(findQuestionButton, new() { Placement = FlyoutPlacementMode.Bottom });
#endif
    }

    private void FindFlyout_QueryCleared(object? sender, EventArgs e)
    {
        if (currentTopic != null)
            dataGrid.ItemsSource = currentTopic.QuestionEntries;
        findTextBlock.Text = string.Empty;
        OnFilter(false);
    }

    private void FindFlyout_QuerySubmitted(object? sender, FindFlyoutQuerySubmittedEventArgs e)
    {
        if (e.FindDirection != FindDirection.All)
            return;
        if (currentTopic != null)
        {
            var entries = new ObservableCollection<QuestionEntry>(from question in currentTopic?.QuestionEntries?.ToList()
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
        switch (e.Context)
        {
            case nameof(QuestionEntry.Title):
                {
                    return question.Title.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };
            case nameof(QuestionEntry.Descriptions):
                {
                    string descriptions = ListToStringConversion.ConvertToString<string>(question.Descriptions, x => x);
                    return descriptions.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };
            case nameof(QuestionEntry.ProbableCases):
                {
                    string probableCases = ListToStringConversion.ConvertToString<string>(question.ProbableCases, x => x);
                    return probableCases.Contains(e.QueryText, e.MatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
                };

            default: return false;
        }
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
        if (currentTopic?.QuestionEntries == null) return;

        foreach (var entry in currentTopic.QuestionEntries)
        {
            entry?.YearsAsked?.Sort(new Comparison<DateOnly>((d1, d2) => d2.CompareTo(d1)));
        }
        HasUnsavedChanges = true;
    }

    private void MoveQuestionUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (CanMoveUp())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int newIndex = Math.Max(dataGrid.SelectedIndex - 1, 0);
            currentTopic?.QuestionEntries?.Move(oldIndex, newIndex);
        }
    }

    private void MoveQuestionDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (CanMoveDown())
        {
            int oldIndex = dataGrid.SelectedIndex;
            int count = currentTopic?.QuestionEntries?.Count - 1 ?? 0;
            int newIndex = Math.Min(dataGrid.SelectedIndex + 1, count);
            currentTopic?.QuestionEntries?.Move(oldIndex, newIndex);
        }
    }

    private bool CanMoveUp() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != 0;

    private bool CanMoveDown() => dataGrid.SelectedItems.Count == 1 && dataGrid.SelectedIndex != currentTopic?.QuestionEntries?.Count - 1;
}
