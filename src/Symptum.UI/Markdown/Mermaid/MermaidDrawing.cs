using Windows.Foundation;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.Text;

namespace Symptum.UI.Markdown.Mermaid;

internal sealed class MermaidDrawingContext
{
    private readonly Dictionary<(string Text, double FontSize, FontWeight Weight), Size> _textCache = new();
    private readonly Canvas _canvas = new();
    private readonly MermaidPalette _palette;
    private readonly double _fontSize;

    public MermaidDrawingContext(MermaidPalette palette, double fontSize)
    {
        _palette = palette;
        _fontSize = fontSize;
    }

    public Canvas Canvas => _canvas;

    public double FontSize => _fontSize;

    public MermaidPalette Palette => _palette;

    public Size MeasureText(string text)
    {
        return MeasureText(text, _fontSize, null);
    }

    public Size MeasureText(string text, FontWeight? weight)
    {
        return MeasureText(text, _fontSize, weight);
    }

    public Size MeasureText(string text, double fontSize, FontWeight? weight = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new Size(0, fontSize);
        }

        var key = (text, fontSize, weight ?? FontWeights.Normal);
        if (_textCache.TryGetValue(key, out Size cached))
        {
            return cached;
        }

        Size measured = MeasureCore(text, fontSize, weight);
        _textCache[key] = measured;
        return measured;
    }

    public TextBlock AddText(
        double x,
        double y,
        string text,
        Brush? brush = null,
        double? fontSize = null,
        FontWeight? weight = null,
        double? maxWidth = null)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize ?? _fontSize,
            Foreground = brush ?? _palette.Text,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = maxWidth.HasValue ? TextWrapping.Wrap : TextWrapping.NoWrap,
            IsTextSelectionEnabled = false
        };

        if (maxWidth.HasValue)
        {
            textBlock.MaxWidth = maxWidth.Value;
        }

        Canvas.SetLeft(textBlock, x);
        Canvas.SetTop(textBlock, y);
        _canvas.Children.Add(textBlock);
        return textBlock;
    }

    public void AddRectangle(
        double x,
        double y,
        double width,
        double height,
        Brush? fill = null,
        Brush? stroke = null,
        double strokeThickness = 1,
        double radiusX = 0,
        double radiusY = 0)
    {
        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            RadiusX = radiusX,
            RadiusY = radiusY
        };

        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        _canvas.Children.Add(rectangle);
    }

    public void AddEllipse(
        double x,
        double y,
        double width,
        double height,
        Brush? fill = null,
        Brush? stroke = null,
        double strokeThickness = 1)
    {
        var ellipse = new Ellipse
        {
            Width = width,
            Height = height,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness
        };

        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        _canvas.Children.Add(ellipse);
    }

    public void AddPolygon(
        IReadOnlyList<Point> points,
        Brush? fill = null,
        Brush? stroke = null,
        double strokeThickness = 1)
    {
        var polygonPoints = new PointCollection();
        foreach (Point point in points)
        {
            polygonPoints.Add(point);
        }

        _canvas.Children.Add(new Polygon
        {
            Points = polygonPoints,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness
        });
    }

    public void AddLine(
        Point start,
        Point end,
        Brush? stroke = null,
        double thickness = 1,
        bool dotted = false)
    {
        var line = new Line
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = stroke ?? _palette.Edge,
            StrokeThickness = thickness
        };

        if (dotted)
        {
            line.StrokeDashArray = [2, 2];
        }

        _canvas.Children.Add(line);
    }

    public void AddArrow(
        Point start,
        Point end,
        Brush? stroke = null,
        double thickness = 1,
        bool dotted = false,
        double arrowLength = 9)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001)
        {
            return;
        }

        double ux = dx / length;
        double uy = dy / length;
        var headStart = new Point(end.X - ux * arrowLength, end.Y - uy * arrowLength);
        AddLine(start, headStart, stroke, thickness, dotted);

        Brush? arrowBrush = stroke ?? _palette.Edge;
        double halfWidth = arrowLength * 0.45;
        AddPolygon(
            [
                end,
                new Point(headStart.X - uy * halfWidth, headStart.Y + ux * halfWidth),
                new Point(headStart.X + uy * halfWidth, headStart.Y - ux * halfWidth)
            ],
            arrowBrush,
            null,
            0);
    }

    public void AddRoundedRect(
        double x,
        double y,
        double width,
        double height,
        Brush? fill = null,
        Brush? stroke = null,
        double strokeThickness = 1,
        double radius = 8)
    {
        AddRectangle(x, y, width, height, fill, stroke, strokeThickness, radius, radius);
    }

    private static readonly TextBlock MeasureProbe = new();

    private static Size MeasureCore(string text, double fontSize, FontWeight? weight)
    {
        MeasureProbe.Text = text;
        MeasureProbe.FontSize = fontSize;
        MeasureProbe.FontWeight = weight ?? FontWeights.Normal;
        MeasureProbe.TextWrapping = TextWrapping.NoWrap;
        MeasureProbe.TextTrimming = TextTrimming.None;
        MeasureProbe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Size measured = MeasureProbe.DesiredSize;
        if (measured.Width <= 0 || measured.Height <= 0)
        {
            measured = new Size(text.Length * fontSize * 0.72, fontSize * 1.5);
        }

        return new Size(
            Math.Max(measured.Width * 1.06, fontSize * 0.6),
            Math.Max(measured.Height, fontSize));
    }
}
