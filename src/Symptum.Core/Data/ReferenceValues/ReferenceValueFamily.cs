using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;

namespace Symptum.Core.Data.ReferenceValues;

public class ReferenceValueFamily : CategoryResource<ReferenceValueGroup>
{
    public ReferenceValueFamily()
    { }

    public ReferenceValueFamily(string title)
    {
        Title = title;
    }

    #region Properties

    [JsonPropertyName("Groups")]
    public override ObservableCollection<ReferenceValueGroup>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }

    #endregion
}
