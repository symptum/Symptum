// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Helpers;
using Symptum.UI.Markdown.TextElements;
using Symptum.UI.Markdown.Renderers.ObjectRenderers;
using Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;
using Symptum.UI.Markdown.Renderers.ObjectRenderers.Extensions;

namespace Symptum.UI.Markdown.Renderers;

public class WinUIRenderer : RendererBase
{
    private readonly Stack<IAddChild> _stack = new();
    private char[] _buffer;
    private MarkdownConfig _config = MarkdownConfig.Default;
    public MyFlowDocument FlowDocument { get; private set; }
    public MarkdownConfig Config
    {
        get => _config;
        set => _config = value;
    }

    public WinUIRenderer(MyFlowDocument document, MarkdownConfig config)
    {
        _buffer = new char[1024];
        Config = config;
        FlowDocument = document;
        // set style
        _stack.Push(FlowDocument);
        LoadOverridenRenderers();
    }

    private void LoadOverridenRenderers()
    {
        LoadRenderers();
    }

    public override object Render(MarkdownObject markdownObject)
    {
        Write(markdownObject);
        return FlowDocument ?? new(Config);
    }

    public void ReloadDocument()
    {
        _stack.Clear();
        FlowDocument.StackPanel.Children.Clear();
        _stack.Push(FlowDocument);
        LoadOverridenRenderers();
    }

    public void WriteLeafInline(LeafBlock leafBlock)
    {
        if (leafBlock == null || leafBlock.Inline == null) throw new ArgumentNullException(nameof(leafBlock));
        Markdig.Syntax.Inlines.Inline? inline = (Markdig.Syntax.Inlines.Inline)leafBlock.Inline;
        while (inline != null)
        {
            Write(inline);
            inline = inline.NextSibling;
        }
    }

    public void WriteLeafRawLines(LeafBlock leafBlock)
    {
        ArgumentNullException.ThrowIfNull(leafBlock);
        if (leafBlock.Lines.Lines != null)
        {
            StringLineGroup lines = leafBlock.Lines;
            StringLine[] slices = lines.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (i != 0)
                    WriteInline(new MyLineBreak());

                WriteText(ref slices[i].Slice);
            }
        }
    }

    public void Push(IAddChild child)
    {
        _stack.Push(child);
    }

    public void Pop()
    {
        IAddChild popped = _stack.Pop();
        _stack.Peek().AddChild(popped);
    }

    public void WriteBlock(IAddChild obj)
    {
        _stack.Peek().AddChild(obj);
    }

    public void WriteInline(IAddChild inline)
    {
        AddInline(_stack.Peek(), inline);
    }

    public void WriteText(ref StringSlice slice)
    {
        if (slice.Start > slice.End)
            return;

        WriteText(slice.Text, slice.Start, slice.Length);
    }

    public void WriteText(string? text)
    {
        WriteInline(new MyInlineText(text ?? ""));
    }

    public void WriteText(string? text, int offset, int length)
    {
        if (text == null)
            return;

        if (offset == 0 && text.Length == length)
        {
            WriteText(text);
        }
        else
        {
            if (length > _buffer.Length)
            {
                _buffer = text.ToCharArray();
                WriteText(new string(_buffer, offset, length));
            }
            else
            {
                text.CopyTo(offset, _buffer, 0, length);
                WriteText(new string(_buffer, 0, length));
            }
        }
    }

    private static void AddInline(IAddChild parent, IAddChild inline)
    {
        parent.AddChild(inline);
    }

    protected virtual void LoadRenderers()
    {
        // Default block renderers
        ObjectRenderers.Add(new CodeBlockRenderer());
        ObjectRenderers.Add(new ListRenderer());
        ObjectRenderers.Add(new HeadingRenderer());
        ObjectRenderers.Add(new ParagraphRenderer());
        ObjectRenderers.Add(new QuoteBlockRenderer());
        ObjectRenderers.Add(new ThematicBreakRenderer());

        // Default inline renderers
        ObjectRenderers.Add(new AutoLinkInlineRenderer());
        ObjectRenderers.Add(new CodeInlineRenderer());
        ObjectRenderers.Add(new DelimiterInlineRenderer());
        ObjectRenderers.Add(new EmphasisInlineRenderer());
        ObjectRenderers.Add(new HtmlEntityInlineRenderer());
        ObjectRenderers.Add(new LineBreakInlineRenderer());
        ObjectRenderers.Add(new LinkInlineRenderer());
        ObjectRenderers.Add(new LiteralInlineRenderer());
        ObjectRenderers.Add(new ContainerInlineRenderer());

        // Extension renderers
        ObjectRenderers.Add(new TableRenderer());
        ObjectRenderers.Add(new TaskListRenderer());
    }
}
