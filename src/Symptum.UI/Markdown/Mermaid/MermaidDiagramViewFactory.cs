using Symptum.Markdown.Mermaid;

namespace Symptum.UI.Markdown.Mermaid;

internal static class MermaidDiagramViewFactory
{
    public static FrameworkElement Create(MermaidDiagramDefinition definition, ElementTheme? requestedTheme = null)
    {
        var palette = MermaidTheme.Create(requestedTheme);

        var root = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = palette.Surface,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 16)
        };

        if (definition is MermaidUnsupportedDiagramDefinition unsupported)
        {
            root.Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(unsupported.Reason)
                    ? "This diagram type is not supported."
                    : unsupported.Reason,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 640,
                Foreground = palette.Text
            };
            return root;
        }

        double fontSize = MermaidTheme.ResolveFontSize();
        var context = new MermaidDrawingContext(palette, fontSize);
        Canvas canvas = MermaidDiagramRenderers.Render(context, definition);

        bool isSequence = definition is MermaidSequenceDiagramDefinition;
        var scrollViewer = new ScrollViewer
        {
            Content = canvas,
            Background = palette.Diagram,
            HorizontalContentAlignment = isSequence ? HorizontalAlignment.Center : HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Auto,
            VerticalScrollMode = ScrollMode.Auto,
            ZoomMode = ZoomMode.Disabled
        };

        if (isSequence)
        {
            scrollViewer.SizeChanged += (_, _) =>
            {
                if (scrollViewer.ViewportWidth <= 0)
                {
                    return;
                }

                double target = scrollViewer.ViewportWidth;
                if (Math.Abs(target - canvas.Width) < 2)
                {
                    return;
                }

                var resizeContext = new MermaidDrawingContext(palette, fontSize);
                canvas = MermaidDiagramRenderers.Render(resizeContext, definition, target);
                scrollViewer.Content = canvas;
            };
        }

        root.Child = scrollViewer;
        return root;
    }
}
