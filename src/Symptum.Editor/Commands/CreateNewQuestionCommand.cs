using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

internal class CreateNewQuestionCommand : IEditorCommand
{
    public string Key => "new-question";

    public Type? EditorPageType { get; } = typeof(QuestionTopicEditorPage);

    public async void Execute(EditorCommandOption? option = null)
    {
        if (EditorPagesManager.CurrentEditor is QuestionTopicEditorPage editorPage &&
            option?.Parameter is QuestionType questionType)
        {
            await editorPage.CreateNewQuestionAsync(questionType);
        }
    }

    public IEnumerable<EditorCommandOption> GetOptions(string? parameter = null)
    {
        return Enum.GetValues<QuestionType>()
            .Select(questionType => new EditorCommandOption(questionType.ToString(), questionType));
    }
}
