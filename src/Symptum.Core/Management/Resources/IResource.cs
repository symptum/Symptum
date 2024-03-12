namespace Symptum.Core.Management.Resources;

public interface IResource
{
    // Will be used mostly for navigation, will be overlapping with Id in most cases but Id will be different in case of cross linking and embedding
    // Eg: Micro/Pedia/CM.ImmunizationSchedule will be different Ids but Uri will be same : mi/sm/notes/immunology/immunizationschedule
    Uri Uri { get; set; } // symptum://subjects/an/sm/notes/abdomen/liver; symptum://subjects/an/qbank/1/abdomen#S_AN_12.3.4

    // Will be used for dependency & resource file resolution and naming of packages
    string Id { get; set; } // AUTOGEN: {Parent.Id}.{Title} -> Subjects.Anatomy.StudyMaterials.Notes.Abdomen.Liver

    string Title { get; set; } // Liver

    IResource? ParentResource { get; } // Id: Subjects.Anatomy.StudyMaterials.Notes.Abdomen

    IList<IResource>? ChildrenResources { get; } // null if end resource

    IList<IResource>? Dependencies { get; set; }

    IList<string>? DependencyIds { get; set; }

    void InitializeResource(IResource? parent);

    bool CanHandleChildResourceType(Type childResourceType);

    bool CanAddChildResourceType(Type childResourceType);

    void AddChildResource(IResource childResource);

    void RemoveChildResource(IResource childResource);

    //IResource GetIResourceFromRelativeUri(Uri relativeUri);

    //IResource GetIResourceFromRelativeId(string relativeId);
}
