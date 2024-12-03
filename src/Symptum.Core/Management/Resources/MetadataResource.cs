using System.Text.Json.Serialization;
using Symptum.Core.Management.Navigation;
using Symptum.Core.Serialization;
using Symptum.Core.Subjects.QuestionBanks;

namespace Symptum.Core.Management.Resources;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(QuestionBank), "questionBank")]
[JsonDerivedType(typeof(CategoryResource), "category")]
[JsonDerivedType(typeof(ImageCategoryResource), "imageCategory")]
[JsonDerivedType(typeof(MarkdownCategoryResource), "markdownCategory")]
public abstract class MetadataResource : NavigableResource, IMetadataResource
{
    #region Properties

    [JsonIgnore]
    public bool SplitMetadata { get; set; } = false;

    [JsonIgnore]
    public string? MetadataPath { get; set; }

    [JsonIgnore]
    public bool IsMetadataLoaded { get; set; } = true;

    #endregion

    internal void LoadMetadata(string metadata)
    {
        if (IsMetadataLoaded) return;
        JsonSerializerEx.PopulateObject(this, metadata);
        IsMetadataLoaded = true;
    }
}

public interface IMetadataResource : IResource
{
    [JsonIgnore]
    public bool SplitMetadata { get; set; }

    [JsonIgnore]
    public string? MetadataPath { get; set; }

    [JsonIgnore]
    public bool IsMetadataLoaded { get; set; }
}
