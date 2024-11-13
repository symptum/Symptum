// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Markdig.Syntax;
using Microsoft.UI.Xaml.Documents;

namespace Symptum.UI.Markdown.TextElements;

internal class MyCodeBlock : IAddChild
{
    private CodeBlock _codeBlock;
    private SContainer _container;
    private MarkdownConfig _config;

    public STextElement TextElement
    {
        get => _container;
    }

    public MyCodeBlock(CodeBlock codeBlock, MarkdownConfig config)
    {
        _codeBlock = codeBlock;
        _config = config;
        _container = new();
        Border border = new()
        {
            Background = (Brush)Application.Current.Resources["ExpanderHeaderBackground"],
            Padding = _config.Themes.CodeBlockPadding,
            Margin = _config.Themes.InternalMargin,
            CornerRadius = _config.Themes.CornerRadius
        };
        TextBlock textBlock = new();

        if (codeBlock is FencedCodeBlock fencedCodeBlock)
        {
            //#if !WINAPPSDK
            //            var formatter = new ColorCode.RichTextBlockFormatter(Extensions.GetOneDarkProStyle());
            //#else
            //            var formatter = new ColorCode.RichTextBlockFormatter(Extensions.GetOneDarkProStyle());
            //#endif
            StringBuilder stringBuilder = new();

            // go through all the lines backwards and only add the lines to a stack if we have encountered the first non-empty line
            Markdig.Helpers.StringLine[] lines = fencedCodeBlock.Lines.Lines;
            Stack<string> stack = new();
            bool encounteredFirstNonEmptyLine = false;
            if (lines != null)
            {
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    Markdig.Helpers.StringLine line = lines[i];
                    if (string.IsNullOrWhiteSpace(line.ToString()) && !encounteredFirstNonEmptyLine)
                    {
                        continue;
                    }

                    encounteredFirstNonEmptyLine = true;
                    stack.Push(line.ToString());
                }

                // append all the lines in the stack to the string builder
                while (stack.Count > 0)
                {
                    stringBuilder.AppendLine(stack.Pop());
                }
            }

            //formatter.FormatRichTextBlock(stringBuilder.ToString(), fencedCodeBlock.ToLanguage(), richTextBlock);
        }
        else
        {
            foreach (Markdig.Helpers.StringLine line in codeBlock.Lines.Lines)
            {
                string lineString = line.ToString();
                if (!string.IsNullOrWhiteSpace(lineString))
                {
                    textBlock.Inlines.Add(new Run() { Text = lineString });
                }
            }
        }
        border.Child = textBlock;
        _container.UIElement = border;
    }

    public void AddChild(IAddChild child) { }
}
