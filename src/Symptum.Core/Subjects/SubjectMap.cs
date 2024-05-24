using System.Collections.ObjectModel;

namespace Symptum.Core.Subjects;

public class SubjectMap
{
    public SubjectMap()
    { }

    static SubjectMap()
    {
        Dictionary<string, SubjectList> codes = new()
        {
            { "", SubjectList.None },
            { "AN", SubjectList.Anatomy },
            { "PY", SubjectList.Physiology },
            { "BI", SubjectList.Biochemistry },
            { "PH", SubjectList.Pharmacology },
            { "PA", SubjectList.Pathology },
            { "MI", SubjectList.Microbiology },
            { "FM", SubjectList.ForensicMedicine },
            { "CM", SubjectList.CommunityMedicine },
            { "EN", SubjectList.OtoRhinoLaryngology },
            { "OP", SubjectList.Ophthalmology },
            { "IM", SubjectList.GeneralMedicine },
            { "SU", SubjectList.GeneralSurgery },
            { "PE", SubjectList.Pediatrics },
            { "OG", SubjectList.ObstetricsAndGynaecology },
        };

        SubjectCodes = new(codes);
    }

    public static readonly ReadOnlyDictionary<string, SubjectList> SubjectCodes;
}
