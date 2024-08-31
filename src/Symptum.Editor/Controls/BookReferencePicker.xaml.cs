using System.Collections;
using Symptum.Core.Subjects.Books;

namespace Symptum.Editor.Controls;

public sealed partial class BookReferencePicker : UserControl
{
    #region Properties

    public static readonly DependencyProperty BookReferenceProperty =
        DependencyProperty.Register(
            nameof(BookReference),
            typeof(BookReference),
            typeof(BookReferencePicker),
            new PropertyMetadata(null, OnBookReferenceChanged));

    private static void OnBookReferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BookReferencePicker bookReferencePicker)
        {
            bookReferencePicker.LoadBookReference();
            bookReferencePicker.UpdatePreviewText();
        }
    }

    public BookReference? BookReference
    {
        get => (BookReference)GetValue(BookReferenceProperty);
        set => SetValue(BookReferenceProperty, value);
    }

    #endregion

    public BookReferencePicker()
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
                || book.Authors.Contains(key, StringComparison.InvariantCultureIgnoreCase) || book.Code.Contains(key, StringComparison.InvariantCultureIgnoreCase));
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
        if (BookReference == null) return;
        selectedBook = BookReference.Book;
#if HAS_UNO_WINUI
        bookQueryBox.Text = selectedBook?.ToString();
#else
        bookList.SelectedItem = selectedBook;
#endif
        editionSelector.Value = BookReference.Edition;
        volumeSelector.Value = BookReference.Volume;
        pageNoSelector.Text = BookReference.PageNumbers;
    }

    private void UpdateBookReference()
    {
        if (BookReference == null) return;
        BookReference.Book = selectedBook;
        BookReference.Edition = (int)editionSelector.Value;
        BookReference.Volume = (int)volumeSelector.Value;
        BookReference.PageNumbers = pageNoSelector.Text;
        UpdatePreviewText();
    }

    private void UpdatePreviewText()
    {
        if (BookReference == null) return;

        string previewText = BookReference.GetPreviewText();
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
