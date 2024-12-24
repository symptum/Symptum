using System.Text.Json.Serialization;

namespace Symptum.Core.Subjects;

[JsonConverter(typeof(JsonStringEnumConverter<SubjectList>))]
public enum SubjectList
{
    None,
    Anatomy,
    Physiology,
    Biochemistry,
    Pharmacology,
    Pathology,
    Microbiology,
    ForensicMedicine,
    CommunityMedicine,
    OtoRhinoLaryngology,
    Ophthalmology,
    GeneralMedicine,
    GeneralSurgery,
    Pediatrics,
    ObstetricsAndGynaecology
}
