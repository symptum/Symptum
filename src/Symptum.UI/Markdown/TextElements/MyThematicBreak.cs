// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Syntax;
using Microsoft.UI;

namespace Symptum.UI.Markdown.TextElements;

internal class MyThematicBreak : IAddChild
{
    private ThematicBreakBlock _thematicBreakBlock;
    private SContainer _container = new();

    public STextElement TextElement
    {
        get => _container;
    }

    public MyThematicBreak(ThematicBreakBlock thematicBreakBlock)
    {
        _thematicBreakBlock = thematicBreakBlock;

        Border border = new()
        {
            BorderThickness = new Thickness(1),
            Margin = new Thickness(8),
            BorderBrush = new SolidColorBrush(Colors.Gray),
            Width = 500,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _container.UIElement = border;
    }

    public void AddChild(IAddChild child) { }
}
