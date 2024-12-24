using Symptum.Core.Data.Bibliography;
using Symptum.Core.Subjects;

namespace Symptum.Editor.Helpers;

internal class QuestionBankContextHelper
{
    public static QuestionBankContext? CurrentContext { get; } = new();
}

internal class QuestionBankContext
{
    public SubjectList? SubjectCode { get; set; }

    public DateOnly? LastInputDate { get; set; }

    public PresetBookReference? PreferredBook { get; set; }
}
