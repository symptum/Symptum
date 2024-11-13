// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;

namespace Symptum.UI.Markdown.TextElements;

internal class MyParagraph : IAddChild
{
    private ParagraphBlock _paragraphBlock;
    private SParagraph _paragraph;

    public STextElement TextElement
    {
        get => _paragraph;
    }

    public MyParagraph(ParagraphBlock paragraphBlock)
    {
        _paragraphBlock = paragraphBlock;
        _paragraph = new();
    }

    public void AddChild(IAddChild child)
    {
        if (child.TextElement is SInline inlineChild)
        {
            _paragraph.Inlines.Add(inlineChild.Inline);
        }
        else if (child.TextElement is SBlock blockChild && blockChild.GetUIElement() is UIElement uiElement)
        {
            _paragraph.UIIndices.Add(_paragraph.Inlines.Count); // Notes the position of the UIElement with respect to the previous inline
            _paragraph.ContainsUI = true;
            _paragraph.UIElements.Add(uiElement);
        }
    }
}
