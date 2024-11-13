// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Markdig.Extensions.TaskLists;
using Microsoft.UI;

namespace Symptum.UI.Markdown.TextElements;

internal class MyTaskListCheckBox : IAddChild
{
    private TaskList _taskList;

    public STextElement TextElement { get; private set; }

    public MyTaskListCheckBox(TaskList taskList)
    {
        _taskList = taskList;
        Grid grid = new()
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(2, 2, 2, 0),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Colors.Gray)
        };
        FontIcon icon = new()
        {
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Glyph = "\uE73E"
        };
        grid.Children.Add(taskList.Checked ? icon : new TextBlock());
        grid.Padding = new Thickness(0);
        grid.CornerRadius = new CornerRadius(2);
        TextElement = new SContainer() { UIElement = grid, PutUIInfront = true };
    }

    public void AddChild(IAddChild child)
    {
    }
}
