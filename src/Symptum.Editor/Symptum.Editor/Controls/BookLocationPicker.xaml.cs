using Symptum.Core.Subjects.Books;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Symptum.Editor.Controls;

public sealed partial class BookLocationPicker : UserControl
{
    #region Properties

    public static readonly DependencyProperty BookLocationProperty =
        DependencyProperty.Register(
            nameof(BookLocation),
            typeof(BookLocation),
            typeof(BookLocationPicker),
            new PropertyMetadata(null, OnBookLocationChanged));

    private static void OnBookLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BookLocationPicker bookLocationPicker)
        {
            bookLocationPicker.LoadBookLocation();
            bookLocationPicker.UpdatePreviewText();
        }
    }

    public BookLocation BookLocation
    {
        get => (BookLocation)GetValue(BookLocationProperty);
        set => SetValue(BookLocationProperty, value);
    }

    #endregion

    public BookLocationPicker()
    {
        InitializeComponent();
        bookSelector.ItemsSource = BookStore.Books;
        bookSelector.TextChanged += bookSelector_TextChanged;
        bookSelector.SuggestionChosen += bookSelector_SuggestionChosen;
        bookSelector.QuerySubmitted += bookSelector_QuerySubmitted;
        UpdatePreviewText();
    }

    private void bookSelector_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suitableItems = new List<Book>();
            var splitText = sender.Text.ToLower().Split(" ");
            foreach (var book in BookStore.Books)
            {
                var found = splitText.All((key) => book.Title.ToLower().Contains(key)
                    || book.Authors.ToLower().Contains(key) || book.Code.ToLower().Contains(key));
                if (found)
                {
                    suitableItems.Add(book);
                }
            }
            if (suitableItems.Count == 0)
            {
            }
            sender.ItemsSource = suitableItems;
        }
    }

    private Book selectedBook;

    private void bookSelector_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        selectedBook = args.SelectedItem as Book;
        sender.Text = selectedBook?.ToString();
    }

    private void bookSelector_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion != null)
        {
            selectedBook = args.ChosenSuggestion as Book;
            sender.Text = selectedBook?.ToString();
        }
    }

    private void LoadBookLocation()
    {
        if (BookLocation == null) return;

        selectedBook = BookLocation.Book;
        bookSelector.Text = selectedBook?.ToString();
        editionSelector.Value = BookLocation.Edition;
        volumeSelector.Value = BookLocation.Volume;
        pageNoSelector.Value = BookLocation.PageNumber;
    }

    private void UpdateBookLocation()
    {
        if (BookLocation == null) return;

        BookLocation.Book = selectedBook;
        BookLocation.Edition = (int)editionSelector.Value;
        BookLocation.Volume = (int)volumeSelector.Value;
        BookLocation.PageNumber = (int)pageNoSelector.Value;
        UpdatePreviewText();
    }

    private void UpdatePreviewText()
    {
        if (BookLocation == null) return;

        string previewText = $"{BookLocation.Book?.Title} by {BookLocation.Book?.Authors}, " +
            $"Edition: {BookLocation.Edition}, Volume: {BookLocation.Volume}, Page Number: {BookLocation.PageNumber}";
        previewTextBlock.Text = previewText;
        ToolTipService.SetToolTip(previewButton, previewText);
    }

    private void Flyout_Opening(object sender, object e)
    {
        LoadBookLocation();
    }

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
        flyout.Hide();
        LoadBookLocation();
    }

    private void okButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateBookLocation();
        flyout.Hide();
    }
}
