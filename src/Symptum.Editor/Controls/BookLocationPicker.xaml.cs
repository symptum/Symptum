using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Symptum.Core.Subjects.Books;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Symptum.Editor.Controls
{
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
            this.InitializeComponent();
            bookSelector.ItemsSource = Book.Books;
            UpdatePreviewText();
        }

        private void LoadBookLocation()
        {
            if (BookLocation == null) return;

            bookSelector.SelectedItem = BookLocation.Book;
            editionSelector.Value = BookLocation.Edition;
            volumeSelector.Value = BookLocation.Volume;
            pageNoSelector.Value = BookLocation.PageNumber;
        }

        private void UpdateBookLocation()
        {
            if (BookLocation == null) return;

            BookLocation.Book = bookSelector.SelectedItem as Book;
            BookLocation.Edition = (int)editionSelector.Value;
            BookLocation.Volume = (int)volumeSelector.Value;
            BookLocation.PageNumber = (int)pageNoSelector.Value;
            UpdatePreviewText();
        }

        private void UpdatePreviewText()
        {
            if (BookLocation == null) return;

            string previewText = $"{BookLocation.Book?.Title} by {BookLocation.Book?.Author}, " +
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
}