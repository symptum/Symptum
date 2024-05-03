using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueParameter : ObservableObject
{
    #region Properties

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private List<ReferenceValueEntry>? entries;

    [TypeConverter(typeof(ReferenceValueEntryListConverter))]
    public List<ReferenceValueEntry>? Entries
    {
        get => entries;
        set => SetProperty(ref entries, value);
    }

    #endregion

    public ReferenceValueParameter()
    {
    }

    public ReferenceValueParameter(string title)
    {
        Title = title;
    }
}
