using CsvHelper;
using Symptum.Core.Subjects.QuestionBank;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static void LoadBooks(string path)
        {
            if (!File.Exists(path) || Path.GetExtension(path).ToLower() != ".csv") return;

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var books = csv.GetRecords<Book>();
            foreach (var book in books)
            {
                Books.Add(book);
            }
        }
    }
}
