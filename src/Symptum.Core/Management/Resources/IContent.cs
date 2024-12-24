using System.Text.Json.Serialization;
using Symptum.Core.Data;

namespace Symptum.Core.Management.Resources;

public interface IContent : IResource
{
    public string? FilePath { get; set; }

    [JsonIgnore]
    public string? FileExtension { get; }

    [JsonIgnore]
    public abstract ContentFileType FileType { get; }

    public string? Description { get; set; }

    public IList<AuthorInfo>? Authors { get; set; }

    public DateOnly? DateModified { get; set; }

    //public IList<string>? References { get; set; } Replace with Book-, Link- and JournalReferences

    public IList<string>? Tags { get; set; }

    public IList<string>? SeeAlso { get; set; }
}
