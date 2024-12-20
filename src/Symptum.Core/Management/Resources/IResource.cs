using System.Text.Json.Serialization;

namespace Symptum.Core.Management.Resources;

public interface IResource
{
    public string? Title { get; set; }

    // Will be used for dependency & resource file resolution and naming of packages
    public string? Id { get; set; } // AUTOGEN: {Parent.Id}.{Title} -> Subjects.Anatomy.StudyMaterials.Notes.Abdomen.Liver

    // Will be used mostly for navigation, will be overlapping with Id in most cases but Id will be different in case of cross linking and embedding
    public Uri? Uri { get; set; } // symptum://subjects/an/sm/notes/abdomen/liver; symptum://subjects/an/qbank/1/abdomen#S_AN_12.3.4

    [JsonIgnore]
    public IResource? ParentResource { get; } // Id: Subjects.Anatomy.StudyMaterials.Notes.Abdomen

    [JsonIgnore]
    public IReadOnlyList<IResource>? ChildrenResources { get; } // null if end resource

    //public IList<IResource>? Dependencies { get; set; }

    //public IList<string>? DependencyIds { get; set; }

    [JsonIgnore]
    public bool CanHandleChildren { get; }

    public void InitializeResource(IResource? parent);

    public bool CanHandleChildResourceType(Type childResourceType);

    public bool CanAddChildResourceType(Type childResourceType);

    public void AddChildResource(IResource childResource);

    public void RemoveChildResource(IResource childResource);
}
