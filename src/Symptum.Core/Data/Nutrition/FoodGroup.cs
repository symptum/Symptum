using CsvHelper;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.Nutrition;

public class FoodGroup : CsvFileResource
{
    public FoodGroup()
    { }

    public FoodGroup(string title)
    {
        Title = title;
    }

    #region Properties

    private ObservableCollection<Food>? foods;

    [JsonIgnore]
    public ObservableCollection<Food>? Foods
    {
        get => foods;
        set => SetProperty(ref foods, value);
    }

    #endregion

    protected override string OnWriteCSV()
    {
        using StringWriter writer = new();
        using CsvWriter csvW = new(writer, CultureInfo.InvariantCulture);
        csvW.WriteHeader<Food>();
        csvW.NextRecord();
        if (Foods != null)
        {
            foreach (var food in Foods)
            {
                csvW.WriteRecord(food);
                csvW.NextRecord();
            }
        }

        return writer.ToString();
    }

    protected override void OnReadCSV(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return;

        using StringReader reader = new(csv);
        using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
        Foods = new(csvReader.GetRecords<Food>().ToList());
    }
}
