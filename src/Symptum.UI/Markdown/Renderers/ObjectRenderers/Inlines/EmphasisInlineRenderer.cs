// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax.Inlines;
using Symptum.UI.Markdown.TextElements;

namespace Symptum.UI.Markdown.Renderers.ObjectRenderers.Inlines;

internal class EmphasisInlineRenderer : WinUIObjectRenderer<EmphasisInline>
{
    protected override void Write(WinUIRenderer renderer, EmphasisInline obj)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(obj);

        MyEmphasisInline? span = null;

        switch (obj.DelimiterChar)
        {
            case '*':
            case '_':
                span = new MyEmphasisInline(obj);
                if (obj.DelimiterCount == 2) { span.SetBold(); } else { span.SetItalic(); }
                break;
            case '~':
                span = new MyEmphasisInline(obj);
                if (obj.DelimiterCount == 2) { span.SetStrikeThrough(); } else { span.SetSubscript(); }
                break;
            case '^':
                span = new MyEmphasisInline(obj);
                span.SetSuperscript();
                break;
        }

        if (span != null)
        {
            renderer.Push(span);
            renderer.WriteChildren(obj);
            renderer.Pop();
        }
        else
        {
            renderer.WriteChildren(obj);
        }
    }
}
