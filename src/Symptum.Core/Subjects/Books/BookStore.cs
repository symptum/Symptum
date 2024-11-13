using System.Collections.ObjectModel;
using System.Globalization;
using CsvHelper;

namespace Symptum.Core.Subjects.Books;

public class BookStore
{
    public static ObservableCollection<Book> Books { get; private set; } = [];

    //public static void SaveBooks(string path)
    //{
    //    using StreamWriter writer = new(path);
    //    using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
    //    csv.WriteHeader<Book>();
    //    csv.NextRecord();
    //    foreach (var book in Books)
    //    {
    //        csv.WriteRecord(book);
    //        csv.NextRecord();
    //    }
    //}

    public static void LoadBooks(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return;

        using StringReader reader = new(csv);
        using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
        var books = csvReader.GetRecords<Book>();
        foreach (var book in books)
        {
            string subCode = book.Id[..2];
            book.Subject = SubjectMap.SubjectCodes.FirstOrDefault(x => x.Key == subCode).Value;
            Books.Add(book);
        }
    }
}
