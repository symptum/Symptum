namespace Symptum.Editor.EditorPages;

public sealed partial class MarkdownEditorPage : EditorPageBase
{
    public MarkdownEditorPage()
    {
        InitializeComponent();
    }

    private string mrkd = @"Hello **bold** `code` Hello **bold** ***italic***`code 2` hello **bold**`code 3` hello **bold**`code 4`

`code 5` Hello **bold** ***italic*** `code 6` `code 7` Hello **bold** ***italic***

# ALERT

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.";

}
