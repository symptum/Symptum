using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace Symptum.Core.QuestionBank
{
    public class QuestionEntry
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        [TypeConverter(typeof(DateOnlyListConverter))]
        public List<DateOnly> YearsAsked { get; set; }

        [TypeConverter(typeof(StringListConverter))]
        public List<string> BookLocations { get; set; }

        public string ProbableCases { get; set; }

        [TypeConverter(typeof(UriListConverter))]
        public List<Uri> ReferenceLinks { get; set; }

        public QuestionEntry()
        {

        }
    }

    public class DateOnlyListConverter : ListConverter<DateOnly>
    {
        public override void ValidateData(string text, List<DateOnly> list)
        {
            if (DateOnly.TryParse(text, out DateOnly year))
            {
                list.Add(year);
            }
        }

        public override string ElementToString(DateOnly data)
        {
            return data.ToString("yyyy-MM");
        }
    }

    public class UriListConverter : ListConverter<Uri>
    {
        public override void ValidateData(string text, List<Uri> list)
        {
            if (!string.IsNullOrEmpty(text))
            {
                list.Add(new Uri(text));
            }
        }
    }

    public class StringListConverter : ListConverter<string>
    {
        public override void ValidateData(string text, List<string> list)
        {
            if (!string.IsNullOrEmpty(text))
            {
                list.Add(text);
            }
        }
    }

    public class ListConverter<T> : DefaultTypeConverter
    {
        private string delimiter = ";";

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text)) return null;

            List<T> list = new();

            string[] values = text.Split(delimiter);

            foreach (var value in values)
            {
                ValidateData(value, list);
            }

            return list;
        }

        public virtual void ValidateData(string text, List<T> list)
        { }

        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            var stringBuilder = new StringBuilder();

            if (value is List<T> values)
            {
                for (int i = 0;  i < values.Count; i++)
                {
                    var data = values[i];
                    stringBuilder.Append(ElementToString(data));
                    if (i < values.Count - 1) stringBuilder.Append(delimiter);
                }
            }

            return stringBuilder.ToString();
        }

        public virtual string ElementToString(T? data) => data?.ToString() ?? string.Empty;
    }
}
