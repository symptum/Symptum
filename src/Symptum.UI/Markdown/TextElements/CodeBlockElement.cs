using System.Text;
using ColorCode.Uno;
using Markdig.Helpers;
using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

public class CodeBlockElement : IAddChild
{
    private CodeBlock _codeBlock;
    private SContainer _container = new();
    private MarkdownConfiguration _config;

    public STextElement TextElement => _container;

    public CodeBlockElement(CodeBlock codeBlock, MarkdownConfiguration config)
    {
        _codeBlock = codeBlock;
        _config = config;
        Border border = new()
        {
            Style = _config.Themes.CodeBlockBorderStyle
        };
        TextBlock textBlock = new()
        {
            Style = config.Themes.CodeTextBlockStyle
        };

        StringBuilder stringBuilder = new();

        if (codeBlock is FencedCodeBlock fencedCodeBlock)
        {
            var formatter = new RichTextBlockFormatter(ElementTheme.Dark);

            // go through all the lines backwards and only add the lines if we have encountered the first non-empty line
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
