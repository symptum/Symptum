namespace Symptum.ViewModels;
public class MainViewModel
{
    public static List<SubjectInfo> Subjects { get; private set; } = new()
        {
            new SubjectInfo("Anatomy", "subjects/anat"), new SubjectInfo("Physiology", "subjects/physio"), new SubjectInfo("Biochemistry", "subjects/biochem"),
            new SubjectInfo("Pathology", "subjects/path"), new SubjectInfo("Pharmacology", "subjects/pharm"), new SubjectInfo("Microbiology", "subjects/micro"),
            new SubjectInfo("ENT", "subjects/ent"), new SubjectInfo("Ophthalmology", "subjects/ophthal"), new SubjectInfo("Forensic Medicine", "subjects/fm"), new SubjectInfo("SPM", "subjects/spm"),
            new SubjectInfo("General Medicine", "subjects/medicine"), new SubjectInfo("General Surgery", "subjects/surgery"), new SubjectInfo("Pediatrics", "subjects/pedia"), new SubjectInfo("OG", "subjects/og")
        };
}

public class SubjectInfo
{
    public SubjectInfo(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public string Name { get; private set; }

    public string Path { get; private set; }
}
