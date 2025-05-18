using Symptum.Core.Subjects.QuestionBanks;
using Symptum.Editor.Pages;

namespace Symptum.Editor.Commands;

internal class CreateNewQuestionCommand : IEditorCommand
{
    public string Key => "new-question";

    public Type? EditorPageType { get; } = typeof(QuestionTopicEditorPage);

    public int NumberOfArguments => 3;

    public async void Execute(IEnumerable<EditorCommandArgument>? args = null)
    {
        if (EditorPagesManager.CurrentEditor is QuestionTopicEditorPage editorPage && args != null)
        {
            if (args.ElementAt(0).Parameter is QuestionType questionType &&
                args.ElementAt(1).Parameter is string title && 
                args.ElementAt(2).Parameter is string pages)
            {
                await editorPage.CreateNewQuestionAsync(questionType, title, pages);
            }   
        }
    }

    public async Task<EditorCommandArgumentRequest?> GetRequestAsync(string? text = null, int argIndex = 0)
    {
        return await Task.Run<EditorCommandArgumentRequest?>(() =>
        {
            return argIndex switch
            {
                0 => new("Question Type", EditorCommandArgumentRequestType.Option, Enum.GetValues<QuestionType>()
           .Select(q => new EditorCommandArgument(q.ToString(), q))),
                1 => new("Title", EditorCommandArgumentRequestType.Text),
                2 => new("Pages", EditorCommandArgumentRequestType.Text),
                _ => null
            };
        });
    }
}
