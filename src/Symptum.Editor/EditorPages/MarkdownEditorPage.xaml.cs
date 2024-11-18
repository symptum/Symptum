using Symptum.UI.Markdown;

namespace Symptum.Editor.EditorPages;

public sealed partial class MarkdownEditorPage : EditorPageBase
{
    public MarkdownEditorPage()
    {
        mrkd = string.Join("\r\n", quotesMD, tabledMd);
        InitializeComponent();
    }

    private string mrkd;
    private string quotesMD = @"# Quotes

> Text that is a quote

## Alerts

> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.
";
    private string tabledMd = @"# Tables

| abc | def | ghi |
|:---:|-----|----:|
|  1  | 2   | 3   |

+---------+---------+
| Header  | Header  |
| Column1 | Column2 |
+=========+=========+
| 1. ab   | > This is a quote
| 2. cde  | > For the second column 
| 3. f    |
+---------+---------+
| Second row spanning
| on two columns
+---------+---------+
| Back    |         |
| to      |         |
| one     |         |
| column  |         | 

+---------+---------+
| This is | a table |
+=========+=========+

+---+---+---+
| AAAAA | B |
+ AAAAA +---+
| AAAAA | C |
+---+---+---+
| D | E | F |
+---+---+---+

+---+---+---+
| AAAAA | B |
+---+---+ B +
| D | E | B |
+ D +---+---+
| D | CCCCC |
+---+---+---+
";

    private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is DocumentNode node && node.Navigate is Action navigate)
        {
            navigate();
        }
    }
}
