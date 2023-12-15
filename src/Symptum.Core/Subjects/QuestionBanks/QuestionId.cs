using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Symptum.Core.Helpers;

namespace Symptum.Core.Subjects.QuestionBanks;

/// <summary>
/// Represents a wrapper class for the unique identifier of a question entry.<br/>
/// The id is in the format of "QT_SC_CN", where "QT" = <see cref="QuestionType"/>, "SC" = Subject Code(<see cref="SubjectList"/>), "CN" = Competency Number of the question.
/// </summary>
public class QuestionId : ObservableObject, IUniqueId, IEquatable<QuestionId>
{
    private static Dictionary<string, QuestionType> questionTypes = new()
    {
        { "E", QuestionType.Essay },
        { "S", QuestionType.ShortNote },
        { "M", QuestionType.MCQ }
    };

    #region Properties

    private QuestionType questionType;

    public QuestionType QuestionType
    {
        get => questionType;
        set
        {
            if (SetProperty(ref questionType, value))
                UpdateIdString();
        }
    }

    private SubjectList subjectCode;

    public SubjectList SubjectCode
    {
        get => subjectCode;
        set
        {
            if (SetProperty(ref subjectCode, value))
                UpdateIdString();
        }
    }

    private string competencyNumbers = string.Empty;

    public string CompetencyNumbers
    {
        get => competencyNumbers;
        set
        {
            if (SetProperty(ref competencyNumbers, value))
                UpdateIdString();
        }
    }

    private string idString = string.Empty;

    public string IdString
    {
        get => idString;
        private set => SetProperty(ref idString, value);
    }

    #endregion

    public QuestionId()
    {
        UpdateIdString();
    }

    static IUniqueId? IUniqueId.Parse(string? idText) => Parse(idText);

    public static QuestionId? Parse(string? idText)
    {
        QuestionId questionId = new();

        if (string.IsNullOrEmpty(idText)) return questionId;

        var values = idText.Split(ParserHelper.QuestionIdDelimiter);
        if (values.Length == 3)
        {
            if (questionTypes.TryGetValue(values[0], out QuestionType type))
            {
                questionId.QuestionType = type;
            }
            if (SubjectMap.SubjectCodes.TryGetValue(values[1], out SubjectList subject))
            {
                questionId.SubjectCode = subject;
            }

            questionId.CompetencyNumbers = values[2];
        }

        return questionId;
    }

    private void UpdateIdString()
    {
        IdString = ToString();
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        string qt = questionTypes.FirstOrDefault(x => x.Value == questionType).Key;
        string sc = SubjectMap.SubjectCodes.FirstOrDefault(x => x.Value == subjectCode).Key;
        sb.Append(qt).Append(ParserHelper.QuestionIdDelimiter).Append(sc).Append(ParserHelper.QuestionIdDelimiter).Append(competencyNumbers);
        return sb.ToString();
    }

    public bool Equals(QuestionId? other)
    {
        if (other == null)
            return false;

        return other.QuestionType == QuestionType && other.SubjectCode == SubjectCode && other.CompetencyNumbers == CompetencyNumbers;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        return Equals(obj as QuestionId);
    }
}
