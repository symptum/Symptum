using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;
using Symptum.Core.Extensions;
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
    public partial string? Id { get; set; }

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    [TypeConverter(typeof(ReferenceValueEntryListConverter))]
    public partial List<ReferenceValueEntry>? Entries { get; set; }

    #endregion

    public ReferenceValueParameter Clone(bool cloneId = false) =>
        new()
        {
            // Id is not cloned by default to ensure uniqueness
            Id = cloneId ? Id : null,
            Title = Title,
            Entries = Entries.CloneList(e => e.Clone())
        };
}
