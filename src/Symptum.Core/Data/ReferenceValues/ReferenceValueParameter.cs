using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using Symptum.Core.TypeConversion;

namespace Symptum.Core.Data.ReferenceValues;

public partial class ReferenceValueParameter : ObservableObject
{
    public ReferenceValueParameter()
    { }

    public ReferenceValueParameter(string title)
    {
        Title = title;
    }

    #region Properties

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    [TypeConverter(typeof(ReferenceValueEntryListConverter))]
    public partial List<ReferenceValueEntry>? Entries { get; set; }

    #endregion
}
