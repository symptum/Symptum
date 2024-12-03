using System.Collections.ObjectModel;
using Symptum.Core.Management.Resources;
using System.Text.Json.Serialization;

namespace Symptum.Core.Data.Nutrition;

public class NutritionDataSet : CategoryResource<FoodGroup>
{
    public NutritionDataSet()
    { }

    public NutritionDataSet(string title)
    {
        Title = title;
    }

    #region Properties

    [JsonPropertyName("Groups")]
    public override ObservableCollection<FoodGroup>? Items
    {
        get => base.Items;
        set => base.Items = value;
    }

    #endregion
}
