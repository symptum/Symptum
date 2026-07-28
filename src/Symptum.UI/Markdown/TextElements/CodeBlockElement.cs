using System.Text;
using ColorCode.Uno;
using Markdig.Helpers;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class CodeBlockElement : IAddChild
{
    private CodeBlock _codeBlock;
    private SContainer _container = new();

    public STextElement TextElement => _container;

    public CodeBlockElement(CodeBlock codeBlock, MarkdownTextBlock control)
    {
        _codeBlock = codeBlock;
        Border border = new()
        {
            Style = control.CodeBlockBorderStyle
        };
        TextBlock textBlock = new()
        {
            Style = control.CodeTextBlockStyle
        };

        StringBuilder stringBuilder = new();

        if (codeBlock is FencedCodeBlock fencedCodeBlock)
        {
            var formatter = new RichTextBlockFormatter(ElementTheme.Dark);

            StringLine[] lines = fencedCodeBlock.Lines.Lines;

            bool encounteredFirstNonEmptyLine = false;
            if (lines != null)
            {
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i].ToString();
                    if (string.IsNullOrWhiteSpace(line) && !encounteredFirstNonEmptyLine)
                    {
                        continue;
                    }

                    encounteredFirstNonEmptyLine = true;
                    stringBuilder.AppendLine(line);
                }
            }

            formatter.FormatInlines(stringBuilder.ToString(), fencedCodeBlock.ToLanguage(), textBlock.Inlines);
        }
        else
        {
            for (int i = 0; i < codeBlock.Lines.Lines.Length; i++)
            {
                string line = codeBlock.Lines.Lines[i].ToString();
                stringBuilder.Append(line);

                if (i < codeBlock.Lines.Lines.Length - 1) stringBuilder.AppendLine();
            }
            textBlock.Text = stringBuilder.ToString();
        }
        border.Child = new ScrollViewer() { Content = textBlock, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        _container.UIElement = border;
    }

    public void AddChild(IAddChild child) { }
}
