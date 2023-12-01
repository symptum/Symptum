using CsvHelper;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Symptum.Core.Subjects.Books
{
    public class BookStore
    {
        public static ObservableCollection<Book> Books { get; private set; } = [];

        public static void SaveBooks(string path)
        {
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteHeader<Book>();
            csv.NextRecord();
            foreach (var book in Books)
            {
                csv.WriteRecord(book);
                csv.NextRecord();
            }
        }

        public static void LoadBooks(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return;

            using var reader = new StringReader(csv);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            var books = csvReader.GetRecords<Book>();
            foreach (var book in books)
            {
                Books.Add(book);
            }
        }
    }
}