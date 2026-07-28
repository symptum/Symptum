using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.ReferenceValues;

public partial class ReferenceValueGroup : CsvFileResource
{
    public ReferenceValueGroup()
    { }

    public ReferenceValueGroup(string title)
    {
        Title = title;
    }

    #region Properties

    [JsonIgnore]
    [ObservableProperty]
    public partial ObservableCollection<ReferenceValueParameter>? Parameters { get; set; }

    #endregion

    protected override string OnWriteCSV()
    {
        using StringWriter writer = new();
        using CsvWriter csvW = new(writer, CultureInfo.InvariantCulture);
        csvW.WriteHeader<ReferenceValueParameter>();
        csvW.NextRecord();
        if (Parameters != null)
        {
            foreach (var parameter in Parameters)
            {
                csvW.WriteRecord(parameter);
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
        Parameters = [.. csvReader.GetRecords<ReferenceValueParameter>()];
    }
}
