using Markdig.Extensions.TaskLists;
using Microsoft.UI;

namespace Symptum.UI.Markdown.TextElements;

public class TaskListCheckBoxElement : IAddChild
{
    private TaskList _taskList;
    private SContainer _container = new();

    public STextElement TextElement
    {
        get => _container;
    }

    public TaskListCheckBoxElement(TaskList taskList)
    {
        _taskList = taskList;
        Grid grid = new()
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(2, 2, 2, 0),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Colors.Gray),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        FontIcon icon = new()
        {
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Glyph = "\uE73E"
        };
        if (taskList.Checked) grid.Children.Add(icon);
        grid.Padding = new Thickness(0);
        grid.CornerRadius = new CornerRadius(2);
        _container.UIElement = grid;
    }

    public void AddChild(IAddChild child)
    {
    }
}
