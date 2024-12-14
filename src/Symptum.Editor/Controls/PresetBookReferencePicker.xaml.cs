using System.Collections;
using Symptum.Core.Data.Bibliography;
using Symptum.Core.Subjects.Books;

namespace Symptum.Editor.Controls;

public sealed partial class PresetBookReferencePicker : UserControl
{
    #region Properties

    public static readonly DependencyProperty PresetBookReferenceProperty =
        DependencyProperty.Register(
            nameof(PresetBookReference),
            typeof(PresetBookReference),
            typeof(PresetBookReferencePicker),
            new PropertyMetadata(null, OnPresetBookReferenceChanged));

    private static void OnPresetBookReferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PresetBookReferencePicker picker)
        {
            picker.LoadBookReference();
            picker.UpdatePreviewText();
        }
    }

    public PresetBookReference? PresetBookReference
    {
        get => (PresetBookReference)GetValue(PresetBookReferenceProperty);
        set => SetValue(PresetBookReferenceProperty, value);
    }

    #endregion

    public PresetBookReferencePicker()
    {
        InitializeComponent();
#if HAS_UNO_WINUI
        bookQueryBox.ItemsSource = BookStore.Books;
        bookQueryBox.SuggestionChosen += BookQueryBox_SuggestionChosen;
#else
        bookList.SelectionChanged += BookList_SelectionChanged;
        //bookQueryBox.ItemsSource = _queries;
#endif
        bookQueryBox.TextChanged += BookQueryBox_TextChanged;
        bookQueryBox.QuerySubmitted += BookQueryBox_QuerySubmitted;
        UpdatePreviewText();
    }

    private void BookQueryBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        selectedBook = args.SelectedItem as Book;
        sender.Text = selectedBook?.ToString();
    }

    private void BookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Book book)
        {
            selectedBook = book;
        }
    }

    private void BookQueryBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
#if HAS_UNO_WINUI
        if (args.ChosenSuggestion != null)
        {
            selectedBook = args.ChosenSuggestion as Book;
            sender.Text = selectedBook?.ToString();
        }
#else
        //if (!string.IsNullOrEmpty(args.QueryText) && !_queries.Contains(args.QueryText))
        //    _queries.Add(args.QueryText);
        SearchBook(args.QueryText);
#endif
    }

    private void BookQueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            SearchBook(sender.Text);
        }
    }

    private Book? selectedBook;
    //private readonly ObservableCollection<string> _queries = [];

    private IEnumerable GetBooksGrouped(IList<Book> books)
    {
        var groups = from book in books
                     group book by book.Subject into g
                     select g;

        return groups;
    }

    private void SearchBook(string? queryText)
    {
        if (!flyout.IsOpen)
        {
#if HAS_UNO_WINUI
            bookQueryBox.ItemsSource = BookStore.Books;
#else
            bookGroupedSource.Source = GetBooksGrouped(BookStore.Books);
#endif
            return;
        }

        var suitableItems = new List<Book>();
        var splitText = queryText.ToLower().Split(" ");
        foreach (var book in BookStore.Books)
        {
            var found = splitText.All((key) => book.Title.Contains(key, StringComparison.InvariantCultureIgnoreCase)
                || book.Authors.Contains(key, StringComparison.InvariantCultureIgnoreCase) || book.Id.Contains(key, StringComparison.InvariantCultureIgnoreCase));
            if (found)
            {
                suitableItems.Add(book);
            }
        }
#if HAS_UNO_WINUI
        bookQueryBox.ItemsSource = suitableItems;
#else
        bookGroupedSource.Source = GetBooksGrouped(suitableItems);
#endif
    }

    private void LoadBookReference()
    {
        if (PresetBookReference == null) return;
        selectedBook = BookStore.Books.FirstOrDefault(x => x.Id == PresetBookReference.Book?.Id);
#if HAS_UNO_WINUI
        bookQueryBox.Text = selectedBook?.ToString();
#else
        bookList.SelectedItem = selectedBook;
#endif
        editionSelector.Value = PresetBookReference.Edition;
        volumeSelector.Value = PresetBookReference.Volume;
        pageNoSelector.Text = PresetBookReference.Pages;
    }

    private void UpdateBookReference()
    {
        if (PresetBookReference == null) return;
        PresetBookReference = PresetBookReference with
        {
            Book = selectedBook,
            Edition = (int)editionSelector.Value,
            Volume = (int)volumeSelector.Value,
            Pages = pageNoSelector.Text
        };
    }

    private void UpdatePreviewText()
    {
        if (PresetBookReference == null) return;

        string previewText = PresetBookReference.GetPreviewText();
        previewTextBlock.Text = previewText;
        ToolTipService.SetToolTip(previewButton, previewText);
    }

    private void Flyout_Opening(object sender, object e)
    {
#if !HAS_UNO_WINUI
        bookGroupedSource.Source = GetBooksGrouped(BookStore.Books);
#endif
        LoadBookReference();

#if !HAS_UNO_WINUI
        bookList.ScrollIntoView(selectedBook);
#endif
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        flyout.Hide();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateBookReference();
        flyout.Hide();
    }

    private void Flyout_Closed(object sender, object e)
    {
        bookQueryBox.Text = string.Empty;
    }
}
