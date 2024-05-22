using Symptum.Core.Subjects;
using Symptum.Core.Subjects.Books;

namespace Symptum.Editor.Helpers;

internal class QuestionBankContextHelper
{
    public static QuestionBankContext? CurrentContext { get; } = new();
}

internal class QuestionBankContext
{
    public SubjectList? SubjectCode { get; set; }

    public DateOnly? LastInputDate { get; set; }

    public BookReference? PreferredBook { get; set; }
}
