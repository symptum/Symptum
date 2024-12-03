using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;
using Symptum.Core.Serialization;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValuesPackage : PackageResource<ReferenceValueFamily>
{
    public ReferenceValuesPackage()
    { }

    public ReferenceValuesPackage(string title)
    {
        Title = title;
    }

    #region Properties

    [JsonPropertyName("Families")]
    [ListOfMetadataResource]
    public override ObservableCollection<ReferenceValueFamily>? Contents
    {
        get => base.Contents;
        set => base.Contents = value;
    }

    #endregion
}
