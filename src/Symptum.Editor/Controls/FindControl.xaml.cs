using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Data;
using Symptum.Editor.Converters;

namespace Symptum.Editor.Controls;

public enum FindDirection
{
    Next,
    Previous,
    All
}

public enum FindError
{
    NoMatches,
    NoMoreMatches
}

public sealed partial class FindControl : UserControl
{
    private readonly ObservableCollection<string> _queries = [];

    public FindControl()
    {
        InitializeComponent();
        queryBox.ItemsSource = _queries;
        queryBox.QuerySubmitted += QueryBox_QuerySubmitted;
        fNextButton.Click += (s, e) => Find(FindDirection.Next);
        fPrevButton.Click += (s, e) => Find(FindDirection.Previous);
        fAllButton.Click += (s, e) => Find(FindDirection.All);
        fClearButton.Click += (s, e) => Clear();

        Binding binding = new() { Path = new(nameof(InfoBar.IsOpen)), Source = errorInfoBar, Converter = new BooleanToVisibilityConverter() };
        errorInfoBar.SetBinding(VisibilityProperty, binding);
    }

    public event EventHandler<FindControlQuerySubmittedEventArgs> QuerySubmitted;

    public event EventHandler<EventArgs> QueryCleared;

    #region Properties

    #region QueryText

    public static readonly DependencyProperty QueryTextProperty =
        DependencyProperty.Register(
            nameof(QueryText),
            typeof(string),
            typeof(FindControl),
            new PropertyMetadata(string.Empty));

    public string QueryText
    {
        get => (string)GetValue(QueryTextProperty);
        set => SetValue(QueryTextProperty, value);
    }

    #endregion

    #region FindContexts

    public static readonly DependencyProperty FindContextsProperty =
        DependencyProperty.Register(
            nameof(FindContexts),
            typeof(IList<string>),
            typeof(FindControl),
            new PropertyMetadata(null));

    public IList<string>? FindContexts
    {
        get => (IList<string>?)GetValue(FindContextsProperty);
        set => SetValue(FindContextsProperty, value);
    }

    #endregion

    #region SelectedContext

    public static readonly DependencyProperty SelectedContextProperty =
        DependencyProperty.Register(
            nameof(SelectedContext),
            typeof(string),
            typeof(FindControl),
            new PropertyMetadata(string.Empty));

    public string SelectedContext
    {
        get => (string)GetValue(SelectedContextProperty);
        set => SetValue(SelectedContextProperty, value);
    }

    #endregion

    #region MatchCase

    public static readonly DependencyProperty MatchCaseProperty =
        DependencyProperty.Register(
            nameof(MatchCase),
            typeof(bool),
            typeof(FindControl),
            new PropertyMetadata(false));

    public bool MatchCase
    {
        get => (bool)GetValue(MatchCaseProperty);
        set => SetValue(MatchCaseProperty, value);
    }

    #endregion

    #region MatchWholeWord

    public static readonly DependencyProperty MatchWholeWordProperty =
        DependencyProperty.Register(
            nameof(MatchWholeWord),
            typeof(bool),
            typeof(FindControl),
            new PropertyMetadata(false));

    public bool MatchWholeWord
    {
        get => (bool)GetValue(MatchWholeWordProperty);
        set => SetValue(MatchWholeWordProperty, value);
    }

    #endregion

    #region FindNextEnabled

    public static readonly DependencyProperty FindNextEnabledProperty =
        DependencyProperty.Register(
            nameof(FindNextEnabled),
            typeof(bool),
            typeof(FindControl),
            new PropertyMetadata(false));

    public bool FindNextEnabled
    {
        get => (bool)GetValue(FindNextEnabledProperty);
        set => SetValue(FindNextEnabledProperty, value);
    }

    #endregion

    #region FindPreviousEnabled

    public static readonly DependencyProperty FindPreviousEnabledProperty =
        DependencyProperty.Register(
            nameof(FindPreviousEnabled),
            typeof(bool),
            typeof(FindControl),
            new PropertyMetadata(false));

    public bool FindPreviousEnabled
    {
        get => (bool)GetValue(FindPreviousEnabledProperty);
        set => SetValue(FindPreviousEnabledProperty, value);
    }

    #endregion

    #region FindPreviousEnabled

    public static readonly DependencyProperty FindAllEnabledProperty =
        DependencyProperty.Register(
            nameof(FindAllEnabled),
            typeof(bool),
            typeof(FindControl),
            new PropertyMetadata(true));

    public bool FindAllEnabled
    {
        get => (bool)GetValue(FindAllEnabledProperty);
        set => SetValue(FindAllEnabledProperty, value);
    }

    #endregion

    #endregion

    public void ShowErrorMessage(FindError findError)
    {
        errorInfoBar.Message = findError switch
        {
            FindError.NoMatches => $"Can't find \"{QueryText}\"",
            FindError.NoMoreMatches => $"No more occurrences of \"{QueryText}\"",
            _ => string.Empty
        };
        errorInfoBar.Severity = findError switch
        {
            FindError.NoMatches => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Warning
        };

        errorInfoBar.IsOpen = true;
    }

    private void QueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrEmpty(args.QueryText) && !_queries.Contains(args.QueryText))
            _queries.Add(args.QueryText);
        QueryText = args.QueryText;
        Find(FindDirection.All);
    }

    private void Find(FindDirection findDirection)
    {
        errorInfoBar.IsOpen = false;
        if (string.IsNullOrEmpty(QueryText) || string.IsNullOrWhiteSpace(QueryText))
        {
            Clear();
            return;
        }

        FindControlQuerySubmittedEventArgs args = new()
        {
            Context = SelectedContext,
            FindDirection = findDirection,
            MatchCase = MatchCase,
            MatchWholeWord = MatchWholeWord,
            QueryText = QueryText
        };

        RaiseQuerySubmitted(args);
    }

    private void Clear()
    {
        QueryText = string.Empty;
        QueryCleared?.Invoke(this, new());
    }

    private void RaiseQuerySubmitted(FindControlQuerySubmittedEventArgs e)
    {
        QuerySubmitted?.Invoke(this, e);
    }
}

public class FindControlQuerySubmittedEventArgs : EventArgs
{
    public string QueryText { get; init; } = string.Empty;

    public FindDirection FindDirection { get; init; } = FindDirection.Next;

    public string Context { get; init; } = string.Empty;

    public bool MatchCase { get; init; } = false;

    public bool MatchWholeWord { get; init; } = false;

    public FindControlQuerySubmittedEventArgs()
    {
    }
}
