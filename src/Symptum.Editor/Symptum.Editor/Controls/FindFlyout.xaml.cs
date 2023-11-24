using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Symptum.Editor.Controls;

public enum FindDirection
{
    Next,
    Previous,
    All
}

public sealed partial class FindFlyout : Flyout
{
    private ObservableCollection<string> _queries = new();

    public FindFlyout()
    {
        this.InitializeComponent();
        this.Opened += FindFlyout_Opened;
        queryBox.ItemsSource = _queries;
        queryBox.TextChanged += (s, e) => QueryText = queryBox.Text;
        queryBox.QuerySubmitted += QueryBox_QuerySubmitted;
        fNextButton.Click += (s, e) => Find(FindDirection.Next);
        fPrevButton.Click += (s, e) => Find(FindDirection.Previous);
        fAllButton.Click += (s, e) => Find(FindDirection.All);
        fContextComboBox.SelectionChanged += (s, e) =>
        {
            if (e.AddedItems.Count > 0)
            {
                SelectedContext = e.AddedItems[0].ToString();
            }
        };
        mCaseButton.Click += (s, e) => MatchCase = mCaseButton.IsChecked ?? false;
        mWordButton.Click += (s, e) => MatchWholeWord = mWordButton.IsChecked ?? false;
    }

    public event EventHandler<FindFlyoutQuerySubmittedEventArgs> QuerySubmitted;

    #region Properties

    #region QueryText

    public static readonly DependencyProperty QueryTextProperty =
        DependencyProperty.Register(
            nameof(QueryText),
            typeof(string),
            typeof(FindFlyout),
            new PropertyMetadata(string.Empty, OnQueryTextChanged));

    private static void OnQueryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindFlyout findFlyout)
        {
            findFlyout.queryBox.Text = e.NewValue.ToString();
        }
    }

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
            typeof(FindFlyout),
            new PropertyMetadata(null, OnFindContextsChanged));

    private static void OnFindContextsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindFlyout findFlyout)
        {
            findFlyout.fContextComboBox.ItemsSource = e.NewValue;
        }
    }

    public IList<string> FindContexts
    {
        get => (IList<string>)GetValue(FindContextsProperty);
        set => SetValue(FindContextsProperty, value);
    }

    #endregion

    #region SelectedContext

    public static readonly DependencyProperty SelectedContextProperty =
        DependencyProperty.Register(
            nameof(SelectedContext),
            typeof(string),
            typeof(FindFlyout),
            new PropertyMetadata(string.Empty, OnSelectedContextChanged));

    private static void OnSelectedContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindFlyout findFlyout)
        {
            var contexts = findFlyout.FindContexts;
            if (contexts != null && findFlyout.fContextComboBox.ItemsSource == contexts)
            {
                foreach (var context in contexts)
                {
                    if (context == e.NewValue.ToString())
                        findFlyout.fContextComboBox.SelectedItem = context;
                    return;
                }
            }

            findFlyout.fContextComboBox.SelectedItem = null;
        }
    }

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
            typeof(FindFlyout),
            new PropertyMetadata(false, OnMatchCaseChanged));

    private static void OnMatchCaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindFlyout findFlyout)
        {
            findFlyout.mCaseButton.IsChecked = (bool)e.NewValue;
        }
    }

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
            typeof(FindFlyout),
            new PropertyMetadata(false, OnMatchWholeWordChanged));

    private static void OnMatchWholeWordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FindFlyout findFlyout)
        {
            findFlyout.mWordButton.IsChecked = (bool)e.NewValue;
        }
    }

    public bool MatchWholeWord
    {
        get => (bool)GetValue(MatchWholeWordProperty);
        set => SetValue(MatchWholeWordProperty, value);
    }

    #endregion

    #endregion

    public void ShowAt(FindOptions findOptions = null, DependencyObject placementTarget = null, FlyoutShowOptions showOptions = null)
    {
        if (showOptions == null)
        {
            showOptions = new()
            {
                Position = new(),
                ShowMode = FlyoutShowMode.Standard,
                Placement = FlyoutPlacementMode.Bottom
            };
        }

        if (findOptions != null)
        {
            QueryText = findOptions.InitialQueryText;
            FindContexts = findOptions.Contexts;
            MatchCase = findOptions.MatchCase;
            MatchWholeWord = findOptions.MatchWholeWord;
        }

        ShowAt(placementTarget, showOptions);
    }

    private void QueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!_queries.Contains(args.QueryText))
            _queries.Add(args.QueryText);
        QueryText = args.QueryText;
        Find(FindDirection.Next);
    }

    private void FindFlyout_Opened(object sender, object e)
    {
        queryBox.Focus(FocusState.Programmatic);
    }

    private void Find(FindDirection findDirection)
    {
        FindFlyoutQuerySubmittedEventArgs args = new()
        {
            Context = SelectedContext,
            FindDirection = findDirection,
            MatchCase = MatchCase,
            MatchWholeWord = MatchWholeWord,
            QueryText = QueryText
        };

        RaiseQuerySubmitted(args);
    }

    private void RaiseQuerySubmitted(FindFlyoutQuerySubmittedEventArgs e)
    {
        QuerySubmitted?.Invoke(this, e);
    }
}

public class FindFlyoutQuerySubmittedEventArgs : EventArgs
{
    public string QueryText { get; init; } = string.Empty;

    public FindDirection FindDirection { get; init; } = FindDirection.Next;

    public string Context { get; init; } = string.Empty;

    public bool MatchCase { get; init; } = false;

    public bool MatchWholeWord { get; init; } = false;

    public FindFlyoutQuerySubmittedEventArgs()
    {
    }
}

public class FindOptions : ObservableObject
{
    public string InitialQueryText { get; init; } = string.Empty;

    public IList<string> Contexts { get; init; } = null;

    public bool MatchCase { get; init; } = false;

    public bool MatchWholeWord { get; init; } = false;

    public FindOptions()
    {
    }
}
