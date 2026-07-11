using Symptum.Core.Data;
using Symptum.Core.Data.ReferenceValues;
using Symptum.Core.Management.Resources;
using Symptum.Core.Math;
using Symptum.Core.Subjects;

namespace Symptum;

public static class MockupData
{
    public static void Initialize()
    {
        var labValues = CreateLabValuesPackage();
        ResourceManager.Resources.Add(labValues);
        ((IResource)labValues).InitializeResource(null);
        InitChildren(labValues);

        var anatomy = CreateAnatomySubject();
        var physiology = CreatePhysiologySubject();
        var pathology = CreatePathologySubject();
        var pharmacology = CreatePharmacologySubject();

        ResourceManager.Resources.Add(anatomy);
        ResourceManager.RegisterResource(anatomy);
        ((IResource)anatomy).InitializeResource(null);
        ResourceManager.Resources.Add(physiology);
        ResourceManager.RegisterResource(physiology);
        ((IResource)physiology).InitializeResource(null);
        ResourceManager.Resources.Add(pathology);
        ResourceManager.RegisterResource(pathology);
        ((IResource)pathology).InitializeResource(null);
        ResourceManager.Resources.Add(pharmacology);
        ResourceManager.RegisterResource(pharmacology);
        ((IResource)pharmacology).InitializeResource(null);

        InitChildren(anatomy);
        InitChildren(physiology);
        InitChildren(pathology);
        InitChildren(pharmacology);
    }

    private static void InitChildren(IResource? parent)
    {
        if (parent?.ChildrenResources == null) return;
        foreach (var child in parent.ChildrenResources)
        {
            child.InitializeResource(parent);
            InitChildren(child);
        }
    }

    private static NumericalValue Interval(double min, double max)
    {
        return new()
        {
            IsInterval = true,
            Minimum = min,
            IncludesMinimum = true,
            Maximum = max,
            IncludesMaximum = true
        };
    }

    private static NumericalValue Single(double value)
    {
        return new() { Value = value };
    }

    private static NumericalValue GtEq(double value)
    {
        return new()
        {
            IsInterval = true,
            Minimum = value,
            IncludesMinimum = true,
            Maximum = double.PositiveInfinity,
            IncludesMaximum = false
        };
    }
    
    private static Quantity Q(double min, double max, string unit) => new(Interval(min, max), unit);
    private static Quantity Qs(double value, string unit) => new(Single(value), unit);
    private static Quantity Qg(double value, string unit) => new(GtEq(value), unit);

    private static ReferenceValuesPackage CreateLabValuesPackage()
    {
        var package = new ReferenceValuesPackage("Laboratory Reference Values")
        {
            Id = "ReferenceValues",
            Uri = ResourceManager.GetAbsoluteUri("references"),
            Description = "Comprehensive laboratory reference values for clinical practice",
            Version = new(1, 0),
            Authors = [new("Symptum Team", "team@symptum.com")],
            Tags = ["laboratory", "reference", "clinical"],
            Contents =
            [
                CreateCBCFamily(),
                CreateBMPFamily(),
                CreateLFTFamily(),
                CreateCoagulationFamily(),
                CreateThyroidFamily(),
            ]
        };

        return package;
    }

    private static ReferenceValueFamily CreateCBCFamily()
    {
        var family = new ReferenceValueFamily("Complete Blood Count (CBC)")
        {
            Id = "ReferenceValues.CBC",
            Uri = ResourceManager.GetAbsoluteUri("reference/cbc"),
            Items =
            [
                new("Red Blood Cell Indices")
                {
                    Id = "ReferenceValues.CBC.RBC",
                    Uri = ResourceManager.GetAbsoluteUri("reference/cbc/rbc"),
                    Parameters =
                    [
                        new("RBC Count")
                        {
                            Entries =
                            [
                                new() { Title = "Male", Quantities = [Q(4.7, 6.1, "M/µL")], Inference = "Normal adult male range" },
                                new() { Title = "Female", Quantities = [Q(4.2, 5.4, "M/µL")], Inference = "Normal adult female range" },
                                new() { Title = "Newborn", Quantities = [Q(4.8, 7.2, "M/µL")], Inference = "Higher at birth, decreases over first weeks" },
                            ]
                        },
                        new("Hemoglobin (Hb)")
                        {
                            Entries =
                            [
                                new() { Title = "Male", Quantities = [Q(13.5, 17.5, "g/dL")], Inference = "Normal adult male range" },
                                new() { Title = "Female", Quantities = [Q(12.0, 16.0, "g/dL")], Inference = "Normal adult female range" },
                                new() { Title = "Newborn", Quantities = [Q(14.0, 24.0, "g/dL")], Inference = "Elevated at birth" },
                            ]
                        },
                        new("Hematocrit (Hct)")
                        {
                            Entries =
                            [
                                new() { Title = "Male", Quantities = [Q(41, 53, "%")] },
                                new() { Title = "Female", Quantities = [Q(36, 46, "%")] },
                            ]
                        },
                        new("MCV")
                        {
                            Entries =
                            [
                                new() { Title = "Adult", Quantities = [Q(80, 100, "fL")], Inference = "Mean corpuscular volume" },
                            ]
                        },
                        new("MCH")
                        {
                            Entries =
                            [
                                new() { Title = "Adult", Quantities = [Q(27, 34, "pg")], Inference = "Mean corpuscular hemoglobin" },
                            ]
                        },
                    ]
                },
                new("White Blood Cell Count")
                {
                    Id = "ReferenceValues.CBC.WBC",
                    Uri = ResourceManager.GetAbsoluteUri("reference/cbc/wbc"),
                    Parameters =
                    [
                        new("Total WBC Count")
                        {
                            Entries =
                            [
                                new() { Title = "Adult", Quantities = [Q(4.5, 11.0, "K/µL")] },
                                new() { Title = "Newborn", Quantities = [Q(9.0, 30.0, "K/µL")], Inference = "Elevated at birth" },
                            ]
                        },
                        new("Neutrophils")
                        {
                            Entries =
                            [
                                new() { Title = "Absolute", Quantities = [Q(1.8, 7.7, "K/µL")] },
                                new() { Title = "Percentage", Quantities = [Q(40, 80, "%")] },
                            ]
                        },
                        new("Lymphocytes")
                        {
                            Entries =
                            [
                                new() { Title = "Absolute", Quantities = [Q(1.0, 4.8, "K/µL")] },
                                new() { Title = "Percentage", Quantities = [Q(20, 40, "%")] },
                            ]
                        },
                        new("Monocytes")
                        {
                            Entries =
                            [
                                new() { Title = "Absolute", Quantities = [Q(0.1, 0.8, "K/µL")] },
                                new() { Title = "Percentage", Quantities = [Q(2, 10, "%")] },
                            ]
                        },
                    ]
                },
                new("Platelet Count")
                {
                    Id = "ReferenceValues.CBC.PLT",
                    Uri = ResourceManager.GetAbsoluteUri("reference/cbc/plt"),
                    Parameters =
                    [
                        new("Platelet Count")
                        {
                            Entries =
                            [
                                new() { Title = "Adult", Quantities = [Q(150, 450, "K/µL")] },
                            ]
                        },
                        new("MPV")
                        {
                            Entries =
                            [
                                new() { Title = "Adult", Quantities = [Q(7.4, 10.4, "fL")], Inference = "Mean platelet volume" },
                            ]
                        },
                    ]
                },
            ]
        };

        return family;
    }

    private static ReferenceValueFamily CreateBMPFamily()
    {
        var family = new ReferenceValueFamily("Basic Metabolic Panel (BMP)")
        {
            Id = "ReferenceValues.BMP",
            Uri = ResourceManager.GetAbsoluteUri("reference/bmp"),
            Items =
            [
                new("Electrolytes")
                {
                    Id = "ReferenceValues.BMP.Electrolytes",
                    Uri = ResourceManager.GetAbsoluteUri("reference/bmp/electrolytes"),
                    Parameters =
                    [
                        new("Sodium (Na)") { Entries = [new() { Title = "Adult", Quantities = [Q(136, 145, "mEq/L")] }] },
                        new("Potassium (K)") { Entries = [new() { Title = "Adult", Quantities = [Q(3.5, 5.1, "mEq/L")], Remarks = "Critical: < 2.5 or > 6.5" }] },
                        new("Chloride (Cl)") { Entries = [new() { Title = "Adult", Quantities = [Q(98, 106, "mEq/L")] }] },
                        new("Bicarbonate (HCO3)") { Entries = [new() { Title = "Adult", Quantities = [Q(22, 29, "mEq/L")] }] },
                    ]
                },
                new("Renal Function")
                {
                    Id = "ReferenceValues.BMP.Renal",
                    Uri = ResourceManager.GetAbsoluteUri("reference/bmp/renal"),
                    Parameters =
                    [
                        new("BUN") { Entries = [new() { Title = "Adult", Quantities = [Q(7, 20, "mg/dL")] }] },
                        new("Creatinine (Cr)")
                        {
                            Entries =
                            [
                                new() { Title = "Male", Quantities = [Q(0.7, 1.3, "mg/dL")] },
                                new() { Title = "Female", Quantities = [Q(0.6, 1.1, "mg/dL")] },
                            ]
                        },
                        new("eGFR") { Entries = [new() { Title = "Adult", Quantities = [Q(90, 120, "mL/min/1.73m²")], Inference = "Normal GFR > 90" }] },
                    ]
                },
                new("Glucose Panel")
                {
                    Id = "ReferenceValues.BMP.Glucose",
                    Uri = ResourceManager.GetAbsoluteUri("reference/bmp/glucose"),
                    Parameters =
                    [
                        new("Fasting Glucose")
                        {
                            Entries =
                            [
                                new() { Title = "Normal", Quantities = [Q(70, 100, "mg/dL")] },
                                new() { Title = "Pre-diabetic", Quantities = [Q(100, 126, "mg/dL")], Inference = "Impaired fasting glucose" },
                                new() { Title = "Diabetic", Quantities = [Qg(126, "mg/dL")], Remarks = "Diagnostic threshold ≥ 126 mg/dL" },
                            ]
                        },
                        new("HbA1c")
                        {
                            Entries =
                            [
                                new() { Title = "Normal", Quantities = [Q(4.0, 5.6, "%")] },
                                new() { Title = "Pre-diabetic", Quantities = [Q(5.7, 6.4, "%")] },
                                new() { Title = "Diabetic", Quantities = [Qs(6.5, "%")], Remarks = "Diagnostic threshold ≥ 6.5%" },
                            ]
                        },
                    ]
                },
            ]
        };

        return family;
    }

    private static ReferenceValueFamily CreateLFTFamily()
    {
        var family = new ReferenceValueFamily("Liver Function Tests (LFT)")
        {
            Id = "ReferenceValues.LFT",
            Uri = ResourceManager.GetAbsoluteUri("reference/lft"),
            Items =
            [
                new("Liver Enzymes")
                {
                    Id = "ReferenceValues.LFT.Enzymes",
                    Uri = ResourceManager.GetAbsoluteUri("reference/lft/enzymes"),
                    Parameters =
                    [
                        new("AST (SGOT)") { Entries = [new() { Title = "Adult", Quantities = [Q(10, 40, "U/L")] }] },
                        new("ALT (SGPT)") { Entries = [new() { Title = "Adult", Quantities = [Q(7, 56, "U/L")] }] },
                        new("ALP") { Entries = [new() { Title = "Adult", Quantities = [Q(44, 147, "U/L")] }] },
                        new("GGT") { Entries = [new() { Title = "Adult", Quantities = [Q(8, 61, "U/L")] }] },
                    ]
                },
                new("Bilirubin")
                {
                    Id = "ReferenceValues.LFT.Bilirubin",
                    Uri = ResourceManager.GetAbsoluteUri("reference/lft/bilirubin"),
                    Parameters =
                    [
                        new("Total Bilirubin") { Entries = [new() { Title = "Adult", Quantities = [Q(0.1, 1.2, "mg/dL")] }] },
                        new("Direct Bilirubin") { Entries = [new() { Title = "Adult", Quantities = [Q(0.0, 0.3, "mg/dL")] }] },
                        new("Indirect Bilirubin") { Entries = [new() { Title = "Adult", Quantities = [Q(0.1, 0.9, "mg/dL")] }] },
                    ]
                },
                new("Proteins")
                {
                    Id = "ReferenceValues.LFT.Proteins",
                    Uri = ResourceManager.GetAbsoluteUri("reference/lft/proteins"),
                    Parameters =
                    [
                        new("Total Protein") { Entries = [new() { Title = "Adult", Quantities = [Q(6.4, 8.3, "g/dL")] }] },
                        new("Albumin") { Entries = [new() { Title = "Adult", Quantities = [Q(3.4, 5.4, "g/dL")] }] },
                    ]
                },
            ]
        };

        return family;
    }

    private static ReferenceValueFamily CreateCoagulationFamily()
    {
        var family = new ReferenceValueFamily("Coagulation Panel")
        {
            Id = "ReferenceValues.Coag",
            Uri = ResourceManager.GetAbsoluteUri("reference/coag"),
            Items =
            [
                new("Coagulation Factors")
                {
                    Id = "ReferenceValues.Coag.Factors",
                    Uri = ResourceManager.GetAbsoluteUri("reference/coag/factors"),
                    Parameters =
                    [
                        new("PT") { Entries = [new() { Title = "Adult", Quantities = [Q(11, 13.5, "sec")] }, new() { Title = "On Warfarin", Quantities = [Q(20, 30, "sec")], Inference = "Therapeutic INR 2-3" }] },
                        new("INR") { Entries = [new() { Title = "Normal", Quantities = [Q(0.8, 1.2, "")] }, new() { Title = "Therapeutic", Quantities = [Q(2.0, 3.0, "")] }] },
                        new("aPTT") { Entries = [new() { Title = "Adult", Quantities = [Q(25, 35, "sec")] }] },
                    ]
                },
            ]
        };

        return family;
    }

    private static ReferenceValueFamily CreateThyroidFamily()
    {
        var family = new ReferenceValueFamily("Thyroid Function Tests (TFT)")
        {
            Id = "ReferenceValues.TFT",
            Uri = ResourceManager.GetAbsoluteUri("reference/tft"),
            Items =
            [
                new("Thyroid Hormones")
                {
                    Id = "ReferenceValues.TFT.Hormones",
                    Uri = ResourceManager.GetAbsoluteUri("reference/tft/hormones"),
                    Parameters =
                    [
                        new("TSH") { Entries = [new() { Title = "Adult", Quantities = [Q(0.4, 4.0, "mIU/L")] }] },
                        new("Free T4") { Entries = [new() { Title = "Adult", Quantities = [Q(0.8, 1.8, "ng/dL")] }] },
                        new("Free T3") { Entries = [new() { Title = "Adult", Quantities = [Q(2.3, 4.2, "pg/mL")] }] },
                    ]
                },
            ]
        };

        return family;
    }

    private static Subject CreateAnatomySubject()
    {
        var subject = new Subject(SubjectList.Anatomy)
        {
            Title = "Anatomy",
            Id = "Subjects.Anatomy",
            Uri = ResourceManager.GetAbsoluteUri("subjects/an"),
            Description = "Human Anatomy is the scientific study of the structure of the human body, including its systems, organs, tissues, and cells. It provides a foundation for understanding the relationships between different body parts and their functions."
        };

        var studyMaterials = new MarkdownCategoryResource
        {
            Title = "Study Materials",
            Id = "Subjects.Anatomy.StudyMaterials",
            Uri = ResourceManager.GetAbsoluteUri("subjects/an/sm"),
        };

        var intro = new MarkdownFileResource
        {
            Title = "Introduction to Human Anatomy",
            Id = "Subjects.Anatomy.StudyMaterials.Intro",
            Uri = ResourceManager.GetAbsoluteUri("subjects/an/sm/intro"),
            Markdown = @"# Introduction to Human Anatomy

Anatomy is the branch of biology concerned with the study of the structure of organisms and their parts.

## Branches of Anatomy

| Branch | Description |
|--------|-------------|
| **Gross Anatomy** | Study of structures visible to the naked eye |
| **Microscopic Anatomy** | Study of structures at the cellular and tissue level |
| **Developmental Anatomy** | Study of structural changes throughout the lifespan |

## Anatomical Position

The body is in the **anatomical position** when standing upright, feet together, arms at the sides, palms facing forward.

### Directional Terms

| Term | Definition |
|------|------------|
| Superior (Cranial) | Toward the head |
| Inferior (Caudal) | Toward the feet |
| Medial | Toward the midline |
| Lateral | Away from the midline |
| Proximal | Closer to the trunk |
| Distal | Farther from the trunk |

## Body Cavities

1. **Dorsal Cavity** – Cranial and spinal cavities
2. **Ventral Cavity** – Thoracic and abdominopelvic cavities

> [!TIP]
> Use the mnemonic **""SCALP""** for layers of the scalp: Skin, Connective tissue, Aponeurosis, Loose areolar tissue, Pericranium.
"
        };

        var upperLimb = new MarkdownFileResource
        {
            Title = "Upper Limb Anatomy",
            Id = "Subjects.Anatomy.StudyMaterials.UpperLimb",
            Uri = ResourceManager.GetAbsoluteUri("subjects/an/sm/upperlimb"),
            Markdown = @"# Upper Limb Anatomy

## Bones

- **Clavicle** – Collarbone
- **Scapula** – Shoulder blade
- **Humerus** – Arm bone
- **Radius** – Lateral forearm
- **Ulna** – Medial forearm

## Major Muscles

| Muscle | Origin | Insertion | Action |
|--------|--------|-----------|--------|
| Deltoid | Clavicle, acromion | Deltoid tuberosity | Abducts arm |
| Biceps Brachii | Supraglenoid tubercle | Radial tuberosity | Flexes elbow, supinates |
| Triceps Brachii | Infraglenoid tubercle | Olecranon | Extends elbow |

## Brachial Plexus

> **Roots → Trunks → Divisions → Cords → Branches**

| Nerve | Root Value | Function |
|-------|-----------|----------|
| Musculocutaneous | C5-C7 | Elbow flexion |
| Median | C5-T1 | Wrist/finger flexion |
| Ulnar | C8-T1 | Intrinsic hand muscles |
| Radial | C5-T1 | Extension |

> [!CAUTION]
> Radial nerve injury → **wrist drop**.
> Median nerve injury → **ape hand deformity**.
> Ulnar nerve injury → **claw hand deformity**.
"
        };

        studyMaterials.Items = [intro, upperLimb];

        var imageLibrary = new ImageCategoryResource
        {
            Title = "Image Library",
            Id = "Subjects.Anatomy.Images",
            Uri = ResourceManager.GetAbsoluteUri("subjects/an/images"),
            Items = [
                new ()
                {
                    Title = "Test Image"
                }
            ]
        };

        subject.Contents = [studyMaterials, imageLibrary];
        return subject;
    }

    private static Subject CreatePhysiologySubject()
    {
        var subject = new Subject(SubjectList.Physiology)
        {
            Title = "Physiology",
            Id = "Subjects.Physiology",
            Uri = ResourceManager.GetAbsoluteUri("subjects/phy"),
            Description = "Human Physiology is the scientific study of the functions and mechanisms of the human body, including how organs, tissues, and cells work together to maintain homeostasis and respond to internal and external stimuli."
        };

        var cellPhys = new MarkdownFileResource
        {
            Title = "Cell Physiology",
            Id = "Subjects.Physiology.StudyMaterials.Cell",
            Uri = ResourceManager.GetAbsoluteUri("subjects/phy/sm/cell"),
            Markdown = @"# Cell Physiology

## Cell Membrane

The cell membrane is a **phospholipid bilayer** with embedded proteins.

### Transport Mechanisms

| Process | Energy | Direction |
|---------|--------|-----------|
| Simple Diffusion | Passive | High → Low concentration |
| Facilitated Diffusion | Passive | Via carrier protein |
| Active Transport | ATP | Low → High concentration |
| Endocytosis | ATP | Into cell via vesicle |
| Exocytosis | ATP | Out of cell via vesicle |

## Resting Membrane Potential

- Typical value: **-70 mV**
- Maintained by Na+/K+ ATPase pump

## Action Potential

> **Phase 1:** Depolarization (Na+ opens) → **Phase 2:** Repolarization (K+ opens) → **Phase 3:** Hyperpolarization → **Phase 4:** Resting

### Refractory Periods

- **Absolute**: No new AP possible
- **Relative**: Stronger stimulus needed

> [!NOTE]
> The absolute refractory period ensures **unidirectional propagation** of action potentials.
"
        };

        var cvPhys = new MarkdownFileResource
        {
            Title = "Cardiovascular Physiology",
            Id = "Subjects.Physiology.StudyMaterials.CV",
            Uri = ResourceManager.GetAbsoluteUri("subjects/phy/sm/cv"),
            Markdown = @"# Cardiovascular Physiology

## Cardiac Cycle

| Phase | Event |
|-------|-------|
| Atrial Systole | Atria contract → ventricles fill |
| Ventricular Systole | Ventricles contract → ejection |
| Diastole | Relaxation → filling |

## Cardiac Output

**CO = HR × SV** (4–8 L/min)

- Heart Rate: 60–100 bpm
- Stroke Volume: ~70 mL

### Frank-Starling Law

> Increased stretch → increased force of contraction

## Blood Pressure

- Systolic: ~120 mmHg
- Diastolic: ~80 mmHg

## Conduction System

1. SA Node (70 bpm) → 2. AV Node (50 bpm) → 3. Bundle of His → 4. Purkinje Fibers

> [!TIP]
> **P** wave = atrial depolarization, **QRS** = ventricular depolarization, **T** wave = ventricular repolarization.
"
        };

        var studyMaterials = new MarkdownCategoryResource
        {
            Title = "Study Materials",
            Id = "Subjects.Physiology.StudyMaterials",
            Uri = ResourceManager.GetAbsoluteUri("subjects/phy/sm"),
            Items = [cellPhys, cvPhys]
        };

        subject.Contents = [studyMaterials];
        return subject;
    }

    private static Subject CreatePathologySubject()
    {
        var subject = new Subject(SubjectList.Pathology)
        {
            Title = "Pathology",
            Id = "Subjects.Pathology",
            Uri = ResourceManager.GetAbsoluteUri("subjects/path"),
            Description = "Pathology is the study of disease, including its causes, mechanisms, and effects on the body."
        };

        var intro = new MarkdownFileResource
        {
            Title = "Introduction to Pathology",
            Id = "Subjects.Pathology.StudyMaterials.Intro",
            Uri = ResourceManager.GetAbsoluteUri("subjects/path/sm/intro"),
            Markdown = @"# Introduction to Pathology

## Cell Injury and Adaptation

| Adaptation | Description |
|------------|-------------|
| Atrophy | Decrease in cell size |
| Hypertrophy | Increase in cell size |
| Hyperplasia | Increase in cell number |
| Metaplasia | Change in cell type |
| Dysplasia | Abnormal cell growth |

## Inflammation

> **Cardinal signs**: Rubor, Calor, Tumor, Dolor, Functio laesa

### Acute vs Chronic

| Feature | Acute | Chronic |
|---------|-------|---------|
| Duration | Days | Months–years |
| Cells | Neutrophils | Macrophages, lymphocytes |
| Outcome | Resolution | Fibrosis |

## Neoplasia

| Feature | Benign | Malignant |
|---------|--------|-----------|
| Growth | Slow | Rapid |
| Borders | Well-defined | Irregular |
| Metastasis | No | Yes |

> [!WARNING]
> Tumor markers alone are NOT diagnostic.
"
        };

        var studyMaterials = new MarkdownCategoryResource
        {
            Title = "Study Materials",
            Id = "Subjects.Pathology.StudyMaterials",
            Uri = ResourceManager.GetAbsoluteUri("subjects/path/sm"),
            Items = [intro]
        };

        subject.Contents = [studyMaterials];
        return subject;
    }

    private static Subject CreatePharmacologySubject()
    {
        var subject = new Subject(SubjectList.Pharmacology)
        {
            Title = "Pharmacology",
            Id = "Subjects.Pharmacology",
            Uri = ResourceManager.GetAbsoluteUri("subjects/pharm"),
            Description = "Pharmacology is the study of drugs and their effects on the body, including their mechanisms of action, therapeutic uses, and potential side effects."
        };

        var aht = new MarkdownFileResource
        {
            Title = "Antihypertensives",
            Id = "Subjects.Pharmacology.StudyMaterials.AHT",
            Uri = ResourceManager.GetAbsoluteUri("subjects/pharm/sm/aht"),
            Markdown = @"# Antihypertensive Drugs

## 1. ACE Inhibitors (-pril)

| Drug | Half-life | Notes |
|------|-----------|-------|
| Captopril | 2h | Short-acting |
| Enalapril | 11h | Prodrug |
| Lisinopril | 12h | Not a prodrug |

**SE**: Cough, angioedema, hyperkalemia, AKI

## 2. ARBs (-sartan)

- Losartan, Valsartan, Candesartan
- Block AT1 receptor
- **No cough** (advantage over ACEi)

## 3. Calcium Channel Blockers

| Type | Drug | Effect |
|------|------|--------|
| DHP | Amlodipine | Vasodilation |
| Non-DHP | Verapamil | ↓ HR, ↓ contractility |

## 4. Beta-Blockers (-olol)

| Drug | Selectivity | Use |
|------|-------------|-----|
| Metoprolol | β1 | HTN, angina, HF |
| Propranolol | β1, β2 | Migraine, tremor |

## 5. Diuretics

| Class | Example | Site |
|-------|---------|------|
| Thiazide | HCTZ | DCT |
| Loop | Furosemide | Loop of Henle |
| K-sparing | Spironolactone | Collecting duct |

> [!TIP]
> **Step therapy**: Start with thiazide, ACEi/ARB, or CCB.
"
        };

        var studyMaterials = new MarkdownCategoryResource
        {
            Title = "Drug Classes",
            Id = "Subjects.Pharmacology.StudyMaterials",
            Uri = ResourceManager.GetAbsoluteUri("subjects/pharm/sm"),
            Items = [aht]
        };

        subject.Contents = [studyMaterials];
        return subject;
    }
}
