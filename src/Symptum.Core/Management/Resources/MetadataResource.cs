using System.Text.Json.Serialization;
using Symptum.Core.Serialization;

namespace Symptum.Core.Management.Resources;

public abstract class MetadataResource : NavigableResource
{
    #region Properties

    [JsonIgnore]
    public bool SplitMetadata { get; set; } = false;

    [JsonIgnore]
    public string? MetadataPath { get; set; }

    [JsonIgnore]
    public bool IsMetadataLoaded { get; internal set; } = true;

    #endregion

    internal void LoadMetadata(string metadata)
    {
        if (IsMetadataLoaded) return;
        JsonSerializerEx.PopulateObject(this, metadata);
        IsMetadataLoaded = true;
    }
}
