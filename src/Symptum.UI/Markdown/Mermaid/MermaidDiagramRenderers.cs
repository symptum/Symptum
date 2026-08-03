using Windows.Foundation;
using Microsoft.UI.Text;
using Symptum.Markdown.Mermaid;
using Windows.UI;

namespace Symptum.UI.Markdown.Mermaid;

internal static class MermaidDiagramRenderers
{
    private const double ContentPadding = 20;
    private const double NodeHorizontalGap = 40;
    private const double NodeVerticalGap = 40;
    private const double ColumnGap = 50;
    private const double MindmapRingGap = 40;
    private const double LabelPadding = 26;
    private const double CornerRadius = 8;

    private readonly record struct NodeSpec(string Id, string Label, MermaidNodeShape Shape);

    public static Canvas Render(MermaidDrawingContext context, MermaidDiagramDefinition definition, double? targetCanvasWidth = null)
    {
        switch (definition.Kind)
        {
            case MermaidDiagramKind.Flowchart:
                RenderFlowchart(context, (MermaidFlowchartDiagramDefinition)definition);
                break;
            case MermaidDiagramKind.StateDiagram:
                RenderState(context, (MermaidStateDiagramDefinition)definition);
                break;
            case MermaidDiagramKind.SequenceDiagram:
                RenderSequence(context, (MermaidSequenceDiagramDefinition)definition, targetCanvasWidth);
                break;
            case MermaidDiagramKind.PieChart:
                RenderPie(context, (MermaidPieDiagramDefinition)definition);
                break;
            case MermaidDiagramKind.QuadrantChart:
                RenderQuadrant(context, (MermaidQuadrantChartDiagramDefinition)definition);
                break;
            case MermaidDiagramKind.Mindmap:
                RenderMindmap(context, (MermaidMindmapDiagramDefinition)definition);
                break;
        }

        return context.Canvas;
    }

    private static void RenderFlowchart(MermaidDrawingContext context, MermaidFlowchartDiagramDefinition definition)
    {
        var nodes = definition.Nodes
            .Select(static node => new NodeSpec(node.Id, node.Label, node.Shape))
            .ToList();

        bool horizontal = definition.Direction is MermaidFlowDirection.LeftToRight or MermaidFlowDirection.RightToLeft;
        bool reverse = definition.Direction is MermaidFlowDirection.RightToLeft or MermaidFlowDirection.BottomToTop;

        (Dictionary<string, Rect> bounds, Dictionary<string, int> rank, Size _) = LayoutFlowchart(context, nodes, definition.Edges, horizontal, reverse);

        var labels = new List<(Point Anchor, string Text, Size Size)>();
        foreach (MermaidFlowEdgeDefinition edge in definition.Edges)
        {
            if (string.IsNullOrEmpty(edge.Label) ||
                !bounds.TryGetValue(edge.FromId, out Rect fromRect) ||
                !bounds.TryGetValue(edge.ToId, out Rect toRect))
            {
                continue;
            }

            Size labelSize = context.MeasureText(edge.Label);
            Point anchor = FlowchartLabelAnchor(fromRect, toRect, horizontal, reverse);
            anchor = NudgeLabelClear(anchor, labelSize, bounds);
            labels.Add((anchor, edge.Label, labelSize));
        }

        double minX = bounds.Values.Min(static rect => rect.Left);
        double minY = bounds.Values.Min(static rect => rect.Top);
        double maxX = bounds.Values.Max(static rect => rect.Right);
        double maxY = bounds.Values.Max(static rect => rect.Bottom);
        foreach ((Point anchor, string text, Size labelSize) in labels)
        {
            minX = Math.Min(minX, anchor.X - labelSize.Width / 2 - 10);
            maxX = Math.Max(maxX, anchor.X + labelSize.Width / 2 + 10);
            minY = Math.Min(minY, anchor.Y - labelSize.Height / 2 - 6);
            maxY = Math.Max(maxY, anchor.Y + labelSize.Height / 2 + 6);
        }

        double offsetX = ContentPadding - minX;
        double offsetY = ContentPadding - minY;
        double width = maxX - minX + ContentPadding * 2;
        double height = maxY - minY + ContentPadding * 2;

        if (offsetX != 0 || offsetY != 0)
        {
            var shifted = new Dictionary<string, Rect>(bounds.Count);
            foreach ((string id, Rect rect) in bounds)
            {
                shifted[id] = new Rect(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
            }

            bounds = shifted;
            labels = labels
                .Select(label => (new Point(label.Anchor.X + offsetX, label.Anchor.Y + offsetY), label.Text, label.Size))
                .ToList();
        }

        context.AddRectangle(0, 0, width, height, context.Palette.Diagram);
        context.Canvas.Width = width;
        context.Canvas.Height = height;

        DrawFlowchartEdges(context, definition.Edges, bounds, rank, horizontal, reverse);

        foreach (NodeSpec node in nodes)
        {
            DrawNode(context, node, bounds[node.Id]);
        }

        foreach ((Point anchor, string text, _) in labels)
        {
            DrawEdgeLabel(context, anchor, text);
        }
    }

    private static void RenderState(MermaidDrawingContext context, MermaidStateDiagramDefinition definition)
    {
        var ordered = OrderStateNodes(definition.States);
        var nodes = ordered
            .Select(static node => new NodeSpec(node.Id, node.Label, node.Shape))
            .ToList();

        var indexOf = new Dictionary<string, int>();
        for (int i = 0; i < ordered.Count; i++)
        {
            indexOf[ordered[i].Id] = i;
        }

        bool horizontal = definition.Direction is MermaidFlowDirection.LeftToRight or MermaidFlowDirection.RightToLeft;

        var edges = new List<(int From, int To, MermaidStateTransitionDefinition Transition)>();
        foreach (MermaidStateTransitionDefinition transition in definition.Transitions)
        {
            if (!indexOf.TryGetValue(transition.FromId, out int fromIndex) ||
                !indexOf.TryGetValue(transition.ToId, out int toIndex))
            {
                continue;
            }

            edges.Add((fromIndex, toIndex, transition));
        }

        int forwardCount = edges.Count(static edge => edge.To > edge.From);
        int backwardCount = edges.Count(static edge => edge.To < edge.From);

        const double TrackSpacing = 12;
        const double Gutter = 28;

        double startX = horizontal ? ContentPadding : ContentPadding + backwardCount * TrackSpacing + Gutter;
        double startY = horizontal ? ContentPadding + backwardCount * TrackSpacing + Gutter : ContentPadding;

        (Dictionary<string, Rect> rawBounds, Size _) = LayoutNodes(context, nodes, definition.Direction, startX, startY);
        double columnWidth = rawBounds.Values.Max(static rect => rect.Width);
        var bounds = new Dictionary<string, Rect>(rawBounds.Count);
        foreach ((string id, Rect rect) in rawBounds)
        {
            bounds[id] = new Rect(rect.X + (columnWidth - rect.Width) / 2, rect.Y, rect.Width, rect.Height);
        }

        foreach ((int fromIndex, int toIndex, MermaidStateTransitionDefinition transition) in edges)
        {
            if (toIndex != fromIndex + 1 || string.IsNullOrEmpty(transition.Label))
            {
                continue;
            }

            Rect fromRect = bounds[transition.FromId];
            Rect toRect = bounds[transition.ToId];
            Size labelSize = context.MeasureText(transition.Label);
            double currentGap = horizontal ? toRect.Left - fromRect.Right : toRect.Top - fromRect.Bottom;
            double requiredGap = horizontal ? labelSize.Width + 16 : labelSize.Height + 16;
            if (requiredGap <= currentGap)
            {
                continue;
            }

            double delta = requiredGap - currentGap;
            foreach (NodeSpec node in nodes)
            {
                if (indexOf[node.Id] <= fromIndex)
                {
                    continue;
                }

                Rect rect = bounds[node.Id];
                bounds[node.Id] = horizontal
                    ? new Rect(rect.X + delta, rect.Y, rect.Width, rect.Height)
                    : new Rect(rect.X, rect.Y + delta, rect.Width, rect.Height);
            }
        }

        double minX = bounds.Values.Min(static rect => rect.Left);
        double maxX = bounds.Values.Max(static rect => rect.Right);
        double minY = bounds.Values.Min(static rect => rect.Top);
        double maxY = bounds.Values.Max(static rect => rect.Bottom);

        var paths = new List<(List<Point> Path, string? Label, bool Dotted, Point? LabelAnchor)>();
        int forwardTrack = 0;
        int backwardTrack = 0;
        foreach ((int fromIndex, int toIndex, MermaidStateTransitionDefinition transition) in edges)
        {
            Rect fromRect = bounds[transition.FromId];
            Rect toRect = bounds[transition.ToId];

            if (fromIndex == toIndex)
            {
                Point? loopAnchor = horizontal
                    ? new Point(fromRect.X + fromRect.Width / 2, fromRect.Top - 26)
                    : new Point(fromRect.Right + 26, fromRect.Y + fromRect.Height / 2);
                paths.Add((BuildStateSelfLoopPath(fromRect, horizontal), transition.Label, transition.Dotted, loopAnchor));
                continue;
            }

            if (toIndex == fromIndex + 1)
            {
                var straight = new List<Point>();
                if (horizontal)
                {
                    straight.Add(new Point(fromRect.Right, fromRect.Y + fromRect.Height / 2));
                    straight.Add(new Point(toRect.Left, toRect.Y + toRect.Height / 2));
                }
                else
                {
                    straight.Add(new Point(fromRect.X + fromRect.Width / 2, fromRect.Bottom));
                    straight.Add(new Point(toRect.X + toRect.Width / 2, toRect.Top));
                }

                paths.Add((straight, transition.Label, transition.Dotted, null));
                continue;
            }

            var path = new List<Point>();
            if (toIndex > fromIndex)
            {
                if (horizontal)
                {
                    double trackY = maxY + Gutter + forwardTrack * TrackSpacing;
                    forwardTrack++;
                    path.Add(new Point(fromRect.X + fromRect.Width / 2, fromRect.Bottom));
                    path.Add(new Point(fromRect.X + fromRect.Width / 2, trackY));
                    path.Add(new Point(toRect.X + toRect.Width / 2, trackY));
                    path.Add(new Point(toRect.X + toRect.Width / 2, toRect.Top));
                }
                else
                {
                    double trackX = maxX + Gutter + forwardTrack * TrackSpacing;
                    forwardTrack++;
                    path.Add(new Point(fromRect.Right, fromRect.Y + fromRect.Height / 2));
                    path.Add(new Point(trackX, fromRect.Y + fromRect.Height / 2));
                    path.Add(new Point(trackX, toRect.Y + toRect.Height / 2));
                    path.Add(new Point(toRect.Right, toRect.Y + toRect.Height / 2));
                }
            }
            else
            {
                if (horizontal)
                {
                    double trackY = minY - Gutter - backwardTrack * TrackSpacing;
                    backwardTrack++;
                    path.Add(new Point(fromRect.X + fromRect.Width / 2, fromRect.Top));
                    path.Add(new Point(fromRect.X + fromRect.Width / 2, trackY));
                    path.Add(new Point(toRect.X + toRect.Width / 2, trackY));
                    path.Add(new Point(toRect.X + toRect.Width / 2, toRect.Top));
                }
                else
                {
                    double trackX = minX - Gutter - backwardTrack * TrackSpacing;
                    backwardTrack++;
                    path.Add(new Point(fromRect.Left, fromRect.Y + fromRect.Height / 2));
                    path.Add(new Point(trackX, fromRect.Y + fromRect.Height / 2));
                    path.Add(new Point(trackX, toRect.Y + toRect.Height / 2));
                    path.Add(new Point(toRect.Left, toRect.Y + toRect.Height / 2));
                }
            }

            paths.Add((path, transition.Label, transition.Dotted, null));
        }

        double extentMinX = minX;
        double extentMaxX = maxX;
        double extentMinY = minY;
        double extentMaxY = maxY;
        foreach ((List<Point> path, string? label, _, Point? labelAnchor) in paths)
        {
            if (string.IsNullOrEmpty(label))
            {
                continue;
            }

            Point anchor = labelAnchor ?? LongestSegmentMidpoint(path);
            Size labelSize = context.MeasureText(label);
            extentMinX = Math.Min(extentMinX, anchor.X - labelSize.Width / 2 - 10);
            extentMaxX = Math.Max(extentMaxX, anchor.X + labelSize.Width / 2 + 10);
            extentMinY = Math.Min(extentMinY, anchor.Y - labelSize.Height / 2 - 6);
            extentMaxY = Math.Max(extentMaxY, anchor.Y + labelSize.Height / 2 + 6);
        }

        double width = extentMaxX - extentMinX + ContentPadding * 2;
        double height = extentMaxY - extentMinY + ContentPadding * 2;
        double offsetX = ContentPadding - extentMinX;
        double offsetY = ContentPadding - extentMinY;

        if (offsetX != 0 || offsetY != 0)
        {
            var shifted = new Dictionary<string, Rect>(bounds.Count);
            foreach ((string id, Rect rect) in bounds)
            {
                shifted[id] = new Rect(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
            }

            bounds = shifted;

            for (int i = 0; i < paths.Count; i++)
            {
                (List<Point> path, string? label, bool dotted, Point? labelAnchor) = paths[i];
                for (int j = 0; j < path.Count; j++)
                {
                    path[j] = new Point(path[j].X + offsetX, path[j].Y + offsetY);
                }

                if (labelAnchor.HasValue)
                {
                    labelAnchor = new Point(labelAnchor.Value.X + offsetX, labelAnchor.Value.Y + offsetY);
                }

                paths[i] = (path, label, dotted, labelAnchor);
            }
        }

        context.AddRectangle(0, 0, width, height, context.Palette.Diagram);
        context.Canvas.Width = width;
        context.Canvas.Height = height;

        foreach ((List<Point> path, string? label, bool dotted, Point? labelAnchor) in paths)
        {
            DrawRoutedEdge(context, path, dotted);

            if (!string.IsNullOrEmpty(label))
            {
                Point anchor = labelAnchor ?? LongestSegmentMidpoint(path);
                DrawEdgeLabel(context, anchor, label);
            }
        }

        foreach (NodeSpec node in nodes)
        {
            DrawNode(context, node, bounds[node.Id]);
        }
    }

    private static List<MermaidStateNodeDefinition> OrderStateNodes(IReadOnlyList<MermaidStateNodeDefinition> states)
    {
        var start = states.FirstOrDefault(static state => state.Id == "__state_start");
        var end = states.FirstOrDefault(static state => state.Id == "__state_end");
        var ordered = new List<MermaidStateNodeDefinition>();
        if (start != null)
        {
            ordered.Add(start);
        }

        ordered.AddRange(states.Where(static state => state.Id != "__state_start" && state.Id != "__state_end"));
        if (end != null)
        {
            ordered.Add(end);
        }

        return ordered;
    }

    private static List<Point> BuildStateSelfLoopPath(Rect rect, bool horizontal)
    {
        var path = new List<Point>();
        if (horizontal)
        {
            double cx = rect.X + rect.Width / 2;
            path.Add(new Point(cx - 8, rect.Top));
            path.Add(new Point(cx - 8, rect.Top - 20));
            path.Add(new Point(cx + 8, rect.Top - 20));
            path.Add(new Point(cx + 8, rect.Top));
        }
        else
        {
            double cy = rect.Y + rect.Height / 2;
            path.Add(new Point(rect.Right, cy - 8));
            path.Add(new Point(rect.Right + 20, cy - 8));
            path.Add(new Point(rect.Right + 20, cy + 8));
            path.Add(new Point(rect.Right, cy + 8));
        }

        return path;
    }

    private static void DrawRoutedEdge(
        MermaidDrawingContext context,
        IReadOnlyList<Point> path,
        bool dotted)
    {
        for (int i = 0; i < path.Count - 2; i++)
        {
            context.AddLine(path[i], path[i + 1], context.Palette.Edge, 1, dotted);
        }

        context.AddArrow(path[^2], path[^1], context.Palette.Edge, 1, dotted);
    }

    private static Point LongestSegmentMidpoint(IReadOnlyList<Point> path)
    {
        double bestLength = 0;
        Point mid = path[0];
        for (int i = 0; i < path.Count - 1; i++)
        {
            double length = Math.Abs(path[i + 1].X - path[i].X) + Math.Abs(path[i + 1].Y - path[i].Y);
            if (length > bestLength)
            {
                bestLength = length;
                mid = new Point((path[i].X + path[i + 1].X) / 2, (path[i].Y + path[i + 1].Y) / 2);
            }
        }

        return mid;
    }

    private static void RenderSequence(MermaidDrawingContext context, MermaidSequenceDiagramDefinition definition, double? targetCanvasWidth = null)
    {
        var idToColumn = new Dictionary<string, int>();
        var columnWidths = new List<double>();

        for (int i = 0; i < definition.Participants.Count; i++)
        {
            MermaidSequenceParticipantDefinition participant = definition.Participants[i];
            Size labelSize = context.MeasureText(participant.Label, FontWeights.SemiBold);
            double width = Math.Max(labelSize.Width + 8, 64) + LabelPadding;
            idToColumn[participant.Id] = i;
            columnWidths.Add(width);
        }

        var gaps = new List<double>();
        for (int i = 1; i < columnWidths.Count; i++)
        {
            gaps.Add(ColumnGap);
        }

        foreach (MermaidSequenceMessageDefinition message in definition.Messages)
        {
            if (string.IsNullOrEmpty(message.Label) ||
                !idToColumn.TryGetValue(message.FromId, out int fromCol) ||
                !idToColumn.TryGetValue(message.ToId, out int toCol))
            {
                continue;
            }

            Size labelSize = context.MeasureText(message.Label);
            if (fromCol == toCol)
            {
                double offset = Math.Min(28, columnWidths[fromCol] / 2 - 6);
                double needed = 2 * (offset + 8 + labelSize.Width) + 8;
                columnWidths[fromCol] = Math.Max(columnWidths[fromCol], needed);
            }
            else
            {
                int lo = Math.Min(fromCol, toCol);
                int hi = Math.Max(fromCol, toCol);
                double required = labelSize.Width + 16;

                double available = 0;
                for (int j = lo; j < hi; j++)
                {
                    available += columnWidths[j] / 2 + gaps[j] + columnWidths[j + 1] / 2;
                }

                if (required > available)
                {
                    gaps[lo] += required - available;
                }
            }
        }

        double naturalWidth = columnWidths.Sum() + gaps.Sum();
        double effectiveWidth = Math.Max(naturalWidth, 480);
        if (targetCanvasWidth.HasValue && columnWidths.Count > 0)
        {
            double targetContent = targetCanvasWidth.Value - ContentPadding * 2;
            if (targetContent > effectiveWidth)
            {
                effectiveWidth = targetContent;
            }
        }

        double leftPad = ContentPadding + (effectiveWidth - naturalWidth) / 2;

        double lineHeight = context.FontSize + 26;
        double topPad = ContentPadding;
        double baselineY = topPad + lineHeight + 12;
        double canvasWidth = effectiveWidth + ContentPadding * 2;

        double CenterOf(string id)
        {
            int index = idToColumn[id];
            double start = 0;
            for (int j = 0; j < index; j++)
            {
                start += columnWidths[j] + gaps[j];
            }

            return leftPad + start + columnWidths[index] / 2;
        }

        var layouts = new List<(MermaidSequenceMessageDefinition Message, bool Valid, double FromCenter, double ToCenter, double Y, int Lines, Size LabelSize, double LabelWidth)>();
        double cursor = baselineY;
        foreach (MermaidSequenceMessageDefinition message in definition.Messages)
        {
            bool valid = idToColumn.ContainsKey(message.FromId) && idToColumn.ContainsKey(message.ToId);
            double fromCenter = valid ? CenterOf(message.FromId) : 0;
            double toCenter = valid ? CenterOf(message.ToId) : 0;

            int lines = 1;
            Size labelSize = default;
            double labelWidth = 0;
            if (valid && !string.IsNullOrEmpty(message.Label))
            {
                labelSize = context.MeasureText(message.Label);
                labelWidth = labelSize.Width;
            }

            double rowHeight = lineHeight * lines;
            double y = cursor + rowHeight / 2;
            layouts.Add((message, valid, fromCenter, toCenter, y, lines, labelSize, labelWidth));
            cursor += rowHeight;
        }

        double bottomY = cursor + ContentPadding;

        context.AddRectangle(0, 0, canvasWidth, bottomY, context.Palette.Diagram);

        foreach (MermaidSequenceParticipantDefinition participant in definition.Participants)
        {
            double center = CenterOf(participant.Id);
            Size labelSize = context.MeasureText(participant.Label, FontWeights.SemiBold);
            context.AddText(center - labelSize.Width / 2, topPad, participant.Label, context.Palette.Text, weight: FontWeights.SemiBold);
            context.AddLine(new Point(center, baselineY - 12), new Point(center, bottomY), context.Palette.NodeStroke, 1, dotted: true);
        }

        foreach ((MermaidSequenceMessageDefinition message, bool valid, double fromCenter, double toCenter, double y, int lines, Size labelSize, double labelWidth) in layouts)
        {
            if (!valid)
            {
                continue;
            }

            if (message.FromId != message.ToId)
            {
                if (message.Emphasized)
                {
                    context.AddArrow(new Point(fromCenter, y), new Point(toCenter, y), context.Palette.Edge, 2, dotted: true);
                }
                else
                {
                    context.AddArrow(new Point(fromCenter, y), new Point(toCenter, y), context.Palette.Edge, 1, message.Dotted);
                }

                if (!string.IsNullOrEmpty(message.Label))
                {
                    double labelHeight = labelSize.Height * lines;
                    double leftBound = Math.Min(fromCenter, toCenter) + 2;
                    double rightBound = Math.Max(fromCenter, toCenter) - 2;
                    double rectX = Math.Clamp(
                        (fromCenter + toCenter) / 2 - labelWidth / 2,
                        leftBound,
                        Math.Max(leftBound, rightBound - labelWidth));
                    context.AddRectangle(rectX, y - labelHeight - 6, labelWidth, labelHeight + 4, context.Palette.Diagram);
                    context.AddText(rectX + 2, y - labelHeight - 4, message.Label, context.Palette.Text, maxWidth: labelWidth);
                }
            }
            else
            {
                double offset = Math.Min(28, columnWidths[idToColumn[message.FromId]] / 2 - 6);
                context.AddLine(new Point(fromCenter + 6, y), new Point(fromCenter + offset, y), context.Palette.Edge, 1, message.Dotted);
                context.AddArrow(new Point(fromCenter + offset, y), new Point(fromCenter + offset, y + 14), context.Palette.Edge, 1, message.Dotted);

                if (!string.IsNullOrEmpty(message.Label))
                {
                    double labelHeight = labelSize.Height * lines;
                    double labelX = Math.Min(
                        fromCenter + offset + 8,
                        Math.Max(4, canvasWidth - labelWidth - 4));
                    context.AddText(labelX, y - labelHeight - 4, message.Label, context.Palette.Text, maxWidth: labelWidth);
                }
            }
        }

        context.Canvas.Width = canvasWidth;
        context.Canvas.Height = bottomY;
    }

    private static void RenderPie(MermaidDrawingContext context, MermaidPieDiagramDefinition definition)
    {
        const double radius = 110;
        double total = definition.Slices.Sum(static slice => slice.Value);
        if (total <= 0)
        {
            total = definition.Slices.Count;
        }

        double centerX = ContentPadding + radius + 4;
        double centerY = ContentPadding + radius + 4;

        double maxLegendWidth = 0;
        foreach (MermaidPieSliceDefinition slice in definition.Slices)
        {
            maxLegendWidth = Math.Max(maxLegendWidth, context.MeasureText(slice.Label).Width);
        }

        double rowHeight = Math.Max(context.FontSize + 12, 26);
        double legendWidth = Math.Max(maxLegendWidth + 110, 140);
        double canvasWidth = ContentPadding * 2 + (radius + 4) * 2 + NodeHorizontalGap + legendWidth;
        double legendHeight = ContentPadding + definition.Slices.Count * rowHeight + ContentPadding;
        double canvasHeight = Math.Max(ContentPadding * 2 + (radius + 4) * 2, legendHeight);

        context.AddRectangle(0, 0, canvasWidth, canvasHeight, context.Palette.Diagram);

        double angle = 0;
        for (int i = 0; i < definition.Slices.Count; i++)
        {
            MermaidPieSliceDefinition slice = definition.Slices[i];
            double sweep = slice.Value / total * 360;
            Brush brush = context.Palette.ChartColors[i % context.Palette.ChartColors.Length];
            AddPieWedge(context, new Point(centerX, centerY), radius, angle, sweep, brush);

            double midDegrees = angle + sweep / 2 - 90;
            double labelRadius = radius * 0.66;
            double lx = centerX + labelRadius * Math.Cos(midDegrees * Math.PI / 180);
            double ly = centerY + labelRadius * Math.Sin(midDegrees * Math.PI / 180);
            string percent = Math.Round(slice.Value / total * 100).ToString() + "%";
            Size pctSize = context.MeasureText(percent);
            context.AddText(lx - pctSize.Width / 2, ly - pctSize.Height / 2, percent, context.Palette.Surface);

            angle += sweep;
        }

        double legendX = ContentPadding * 2 + (radius + 4) * 2 + NodeHorizontalGap;
        double legendY = ContentPadding;
        for (int i = 0; i < definition.Slices.Count; i++)
        {
            MermaidPieSliceDefinition slice = definition.Slices[i];
            Brush brush = context.Palette.ChartColors[i % context.Palette.ChartColors.Length];
            context.AddRectangle(legendX, legendY + (rowHeight - 12) / 2, 12, 12, brush);
            Size labelSize = context.MeasureText(slice.Label);
            context.AddText(legendX + 18, legendY + (rowHeight - labelSize.Height) / 2, slice.Label, context.Palette.Text);
            string valueText = definition.ShowData ? slice.Value.ToString("0.##") : Math.Round(slice.Value / total * 100).ToString() + "%";
            Size valueSize = context.MeasureText(valueText);
            context.AddText(legendX + legendWidth - valueSize.Width, legendY + (rowHeight - valueSize.Height) / 2, valueText, context.Palette.MetaText);
            legendY += rowHeight;
        }

        context.Canvas.Width = canvasWidth;
        context.Canvas.Height = canvasHeight;
    }

    private static void RenderQuadrant(MermaidDrawingContext context, MermaidQuadrantChartDiagramDefinition definition)
    {
        const double plotSize = 320;
        const double topMargin = 48;
        const double leftMargin = 48;
        const double bottomMargin = 40;
        const double rightMargin = 72;

        double originX = leftMargin;
        double originY = topMargin;
        double width = leftMargin + plotSize + rightMargin;
        double height = topMargin + plotSize + bottomMargin;

        context.AddRectangle(0, 0, width, height, context.Palette.Diagram);
        double centerX = originX + plotSize / 2;
        double centerY = originY + plotSize / 2;

        DrawQuadrantFill(context, originX, originY, centerX, centerY, context.Palette.ChartColors[0]);
        DrawQuadrantFill(context, centerX, originY, originX + plotSize, centerY, context.Palette.ChartColors[1]);
        DrawQuadrantFill(context, originX, centerY, centerX, originY + plotSize, context.Palette.ChartColors[3]);
        DrawQuadrantFill(context, centerX, centerY, originX + plotSize, originY + plotSize, context.Palette.ChartColors[2]);

        context.AddLine(new Point(originX, centerY), new Point(originX + plotSize, centerY), context.Palette.NodeStroke, 1);
        context.AddLine(new Point(centerX, originY), new Point(centerX, originY + plotSize), context.Palette.NodeStroke, 1);

        context.AddText(originX + plotSize - context.MeasureText(definition.XRightLabel).Width, originY + plotSize + 6, definition.XRightLabel, context.Palette.MetaText);
        context.AddText(originX, originY + plotSize + 6, definition.XLeftLabel, context.Palette.MetaText);
        context.AddText(originX - 6 - context.MeasureText(definition.YTopLabel).Width, originY, definition.YTopLabel, context.Palette.MetaText);
        context.AddText(originX - 6 - context.MeasureText(definition.YBottomLabel).Width, originY + plotSize - context.FontSize, definition.YBottomLabel, context.Palette.MetaText);

        if (definition.QuadrantLabels.Count >= 4)
        {
            context.AddText(originX + 8, originY + 4, definition.QuadrantLabels[1], context.Palette.MetaText);
            context.AddText(centerX + 8, originY + 4, definition.QuadrantLabels[0], context.Palette.MetaText);
            context.AddText(originX + 8, centerY + 6, definition.QuadrantLabels[3], context.Palette.MetaText);
            context.AddText(centerX + 8, centerY + 6, definition.QuadrantLabels[2], context.Palette.MetaText);
        }

        int colorIndex = 0;
        foreach (MermaidQuadrantPointDefinition point in definition.Points)
        {
            double px = originX + point.X * plotSize;
            double py = originY + (1 - point.Y) * plotSize;
            double radius = Math.Max(4, point.Radius);
            Brush? fill = point.FillColor.HasValue
                ? new SolidColorBrush(ToWindowsColor(point.FillColor.Value))
                : context.Palette.ChartColors[colorIndex++ % context.Palette.ChartColors.Length];
            Brush? stroke = point.StrokeColor.HasValue
                ? new SolidColorBrush(ToWindowsColor(point.StrokeColor.Value))
                : context.Palette.Surface;
            double strokeWidth = point.StrokeWidth > 0 ? point.StrokeWidth : 1.5;

            context.AddEllipse(px - radius / 2, py - radius / 2, radius, radius, fill, stroke, strokeWidth);

            if (!string.IsNullOrEmpty(point.Label))
            {
                Size labelSize = context.MeasureText(point.Label);
                context.AddText(px + radius / 2 + 4, py - labelSize.Height / 2, point.Label, context.Palette.Text);
            }
        }

        context.Canvas.Width = width;
        context.Canvas.Height = height;
    }

    private static void DrawQuadrantFill(MermaidDrawingContext context, double x1, double y1, double x2, double y2, Brush fill)
    {
        if (fill is SolidColorBrush solid && solid.Color.A > 0)
        {
            fill = new SolidColorBrush(Color.FromArgb(54, solid.Color.R, solid.Color.G, solid.Color.B));
        }

        context.AddRectangle(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1), fill);
    }

    private static void RenderMindmap(MermaidDrawingContext context, MermaidMindmapDiagramDefinition definition)
    {
        (Size size, Dictionary<string, Rect> bounds) = LayoutMindmapRadial(context, definition.Root);
        context.AddRectangle(0, 0, size.Width, size.Height, context.Palette.Diagram);
        DrawMindmapEdges(context, definition.Root, bounds);
        DrawMindmapNode(context, definition.Root, bounds);

        context.Canvas.Width = size.Width;
        context.Canvas.Height = size.Height;
    }

    private static void DrawEdgeLabel(MermaidDrawingContext context, Point center, string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        Size labelSize = context.MeasureText(label);
        const double padX = 10;
        const double padY = 6;

        context.AddRectangle(
            center.X - labelSize.Width / 2 - padX,
            center.Y - labelSize.Height / 2 - padY,
            labelSize.Width + padX * 2,
            labelSize.Height + padY * 2,
            context.Palette.Diagram);
        context.AddText(center.X - labelSize.Width / 2, center.Y - labelSize.Height / 2, label, context.Palette.Text);
    }

    private static void DrawAdjacentEdge(
        MermaidDrawingContext context,
        Point start,
        Point end,
        bool dotted,
        bool horizontal)
    {
        if (Math.Abs(start.X - end.X) < 0.001 || Math.Abs(start.Y - end.Y) < 0.001)
        {
            context.AddArrow(start, end, context.Palette.Edge, 1, dotted);
            return;
        }

        var path = new List<Point>();
        if (horizontal)
        {
            path.Add(start);
            path.Add(new Point((start.X + end.X) / 2, start.Y));
            path.Add(new Point((start.X + end.X) / 2, end.Y));
            path.Add(end);
        }
        else
        {
            double midY = (start.Y + end.Y) / 2;
            path.Add(start);
            path.Add(new Point(start.X, midY));
            path.Add(new Point(end.X, midY));
            path.Add(end);
        }

        DrawRoutedEdge(context, path, dotted);
    }

    private static void AddCurvedArrow(
        MermaidDrawingContext context,
        Point start,
        Point end,
        bool dotted,
        bool horizontal)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1)
        {
            return;
        }

        double signX = Math.Abs(dx) > 0.001 ? Math.Sign(dx) : 1;
        double signY = Math.Abs(dy) > 0.001 ? Math.Sign(dy) : 1;
        double delta = Math.Max(30, length * 0.35);

        Point c1 = new(start.X + signX * delta, start.Y);
        Point c2 = horizontal
            ? new Point(end.X - signX * delta, end.Y)
            : new Point(end.X, end.Y - signY * delta);

        var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
        figure.Segments.Add(new BezierSegment { Point1 = c1, Point2 = c2, Point3 = end });

        var path = new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = new PathGeometry { Figures = { figure } },
            Stroke = context.Palette.Edge,
            StrokeThickness = 1
        };
        if (dotted)
        {
            path.StrokeDashArray = [2, 2];
        }

        context.Canvas.Children.Add(path);

        double tangentX = end.X - c2.X;
        double tangentY = end.Y - c2.Y;
        double tangentLength = Math.Sqrt(tangentX * tangentX + tangentY * tangentY);
        if (tangentLength > 0.001)
        {
            double ux = tangentX / tangentLength;
            double uy = tangentY / tangentLength;
            const double arrowLength = 9;
            var headStart = new Point(end.X - ux * arrowLength, end.Y - uy * arrowLength);
            context.AddLine(headStart, end, context.Palette.Edge, 1, dotted);
            double half = arrowLength * 0.45;
            context.AddPolygon(
                [
                    end,
                    new Point(headStart.X - uy * half, headStart.Y + ux * half),
                    new Point(headStart.X + uy * half, headStart.Y - ux * half)
                ],
                context.Palette.Edge,
                null,
                0);
        }
    }

    private static void DrawNode(MermaidDrawingContext context, NodeSpec node, Rect rect)
    {
        switch (node.Shape)
        {
            case MermaidNodeShape.Rounded:
                context.AddRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, context.Palette.NodeFill, context.Palette.NodeStroke, 1, CornerRadius);
                break;
            case MermaidNodeShape.Rectangle:
                context.AddRectangle(rect.X, rect.Y, rect.Width, rect.Height, context.Palette.NodeFill, context.Palette.NodeStroke, 1);
                break;
            case MermaidNodeShape.Circle:
                context.AddEllipse(rect.X, rect.Y, rect.Width, rect.Height, context.Palette.NodeFill, context.Palette.NodeStroke, 1);
                break;
            case MermaidNodeShape.Diamond:
                var points = new List<Point>
                {
                    new(rect.X + rect.Width / 2, rect.Y),
                    new(rect.X + rect.Width, rect.Y + rect.Height / 2),
                    new(rect.X + rect.Width / 2, rect.Y + rect.Height),
                    new(rect.X, rect.Y + rect.Height / 2)
                };
                context.AddPolygon(points, context.Palette.NodeFill, context.Palette.NodeStroke, 1);
                break;
        }

        Size labelSize = context.MeasureText(node.Label);
        context.AddText(
            rect.X + (rect.Width - labelSize.Width) / 2,
            rect.Y + (rect.Height - labelSize.Height) / 2,
            node.Label,
            context.Palette.Text);
    }

    private static (Dictionary<string, Rect> Bounds, Size Size) LayoutNodes(
        MermaidDrawingContext context,
        IReadOnlyList<NodeSpec> nodes,
        MermaidFlowDirection direction,
        double startX = ContentPadding,
        double startY = ContentPadding)
    {
        var bounds = new Dictionary<string, Rect>();
        bool horizontal = direction is MermaidFlowDirection.LeftToRight or MermaidFlowDirection.RightToLeft;

        double x = startX;
        double y = startY;
        double maxWidth = 0;
        double maxHeight = 0;

        foreach (NodeSpec node in nodes)
        {
            Size size = MeasureNode(context, node);
            var rect = new Rect(x, y, size.Width, size.Height);
            bounds[node.Id] = rect;

            if (horizontal)
            {
                x += size.Width + NodeHorizontalGap;
                maxHeight = Math.Max(maxHeight, size.Height);
            }
            else
            {
                y += size.Height + NodeVerticalGap;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
        }

        Size layout = horizontal
            ? new Size(x + ContentPadding, maxHeight + ContentPadding * 2)
            : new Size(maxWidth + ContentPadding * 2, y + ContentPadding);

        return (bounds, layout);
    }

    private static Size MeasureNode(MermaidDrawingContext context, NodeSpec node)
    {
        Size labelSize = context.MeasureText(node.Label);
        switch (node.Shape)
        {
            case MermaidNodeShape.Circle:
                double diameter = Math.Max(labelSize.Width + 20, 44);
                return new Size(diameter, diameter);
            case MermaidNodeShape.Diamond:
                return new Size(labelSize.Width + LabelPadding, labelSize.Height + LabelPadding);
            default:
                return new Size(labelSize.Width + LabelPadding, labelSize.Height + 16);
        }
    }

    private static (Dictionary<string, Rect> Bounds, Dictionary<string, int> Rank, Size Layout) LayoutFlowchart(
        MermaidDrawingContext context,
        IReadOnlyList<NodeSpec> nodes,
        IReadOnlyList<MermaidFlowEdgeDefinition> edges,
        bool horizontal,
        bool reverse)
    {
        var sizes = new Dictionary<string, Size>();
        var order = new Dictionary<string, int>();
        for (int i = 0; i < nodes.Count; i++)
        {
            sizes[nodes[i].Id] = MeasureNode(context, nodes[i]);
            order[nodes[i].Id] = i;
        }

        var successors = nodes.ToDictionary(static node => node.Id, _ => new List<string>());
        var predecessors = nodes.ToDictionary(static node => node.Id, _ => new List<string>());
        foreach (MermaidFlowEdgeDefinition edge in edges)
        {
            if (!successors.TryGetValue(edge.FromId, out List<string>? targets) || !successors.ContainsKey(edge.ToId))
            {
                continue;
            }

            if (!targets.Contains(edge.ToId))
            {
                targets.Add(edge.ToId);
                predecessors[edge.ToId].Add(edge.FromId);
            }
        }

        Dictionary<string, int> rank = RankFlowchart(nodes, successors);
        Dictionary<int, List<string>> ranks = BuildRanks(nodes, rank, order);
        OrderRanks(ranks, predecessors, order);

        int maxRank = ranks.Keys.Max();
        double gap = horizontal ? NodeVerticalGap : NodeHorizontalGap;
        var rankSpans = new Dictionary<int, double>();
        var rankThicknesses = new Dictionary<int, double>();
        foreach ((int r, List<string> ids) in ranks)
        {
            double span = 0;
            double thickness = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                Size size = sizes[ids[i]];
                span += (horizontal ? size.Height : size.Width) + (i < ids.Count - 1 ? gap : 0);
                thickness = Math.Max(thickness, horizontal ? size.Width : size.Height);
            }

            rankSpans[r] = span;
            rankThicknesses[r] = thickness;
        }

        double maxSpan = rankSpans.Values.Max();
        var gutters = new Dictionary<int, double>();
        for (int r = 0; r < maxRank; r++)
        {
            double need = ColumnGap;
            foreach (MermaidFlowEdgeDefinition edge in edges)
            {
                if (string.IsNullOrEmpty(edge.Label) ||
                    !rank.TryGetValue(edge.FromId, out int fromR) ||
                    !rank.TryGetValue(edge.ToId, out int toR) ||
                    fromR != r || toR != r + 1)
                {
                    continue;
                }

                Size labelSize = context.MeasureText(edge.Label);
                need = Math.Max(need, labelSize.Width + 28);
            }

            gutters[r] = need;
        }

        var rankStarts = new Dictionary<int, double>();
        double cursor = ContentPadding;
        for (int r = 0; r <= maxRank; r++)
        {
            rankStarts[r] = cursor;
            double gutter = r < maxRank ? gutters[r] : 0;
            cursor += (rankThicknesses.TryGetValue(r, out double thickness) ? thickness : 0) + gutter;
        }

        double totalStack = cursor + ContentPadding;

        var bounds = new Dictionary<string, Rect>();
        foreach ((int r, List<string> ids) in ranks)
        {
            double cross = rankStarts[r];
            double within = ContentPadding + (maxSpan - rankSpans[r]) / 2;
            for (int i = 0; i < ids.Count; i++)
            {
                Size size = sizes[ids[i]];
                double crossSize = horizontal ? size.Width : size.Height;
                double crossPos = cross + (rankThicknesses[r] - crossSize) / 2;
                if (reverse)
                {
                    crossPos = totalStack - crossPos - crossSize;
                }

                if (horizontal)
                {
                    bounds[ids[i]] = new Rect(crossPos, within, size.Width, size.Height);
                    within += size.Height + gap;
                }
                else
                {
                    bounds[ids[i]] = new Rect(within, crossPos, size.Width, size.Height);
                    within += size.Width + gap;
                }
            }
        }

        double mainExtent = maxSpan + ContentPadding * 2;
        Size layout = horizontal
            ? new Size(totalStack, mainExtent)
            : new Size(mainExtent, totalStack);

        return (bounds, rank, layout);
    }

    private static Dictionary<string, int> RankFlowchart(IReadOnlyList<NodeSpec> nodes, Dictionary<string, List<string>> successors)
    {
        var rank = nodes.ToDictionary(static node => node.Id, _ => 0);

        var inDegree = nodes.ToDictionary(static node => node.Id, _ => 0);
        foreach (List<string> targets in successors.Values)
        {
            foreach (string target in targets)
            {
                inDegree[target]++;
            }
        }

        var roots = new List<string>();
        foreach (NodeSpec node in nodes)
        {
            if (inDegree[node.Id] == 0)
            {
                roots.Add(node.Id);
            }
        }

        if (roots.Count == 0)
        {
            roots.Add(nodes[0].Id);
        }

        var visited = new HashSet<string>();
        var queue = new Queue<(string Id, int Rank)>();
        foreach (string root in roots)
        {
            if (visited.Add(root))
            {
                queue.Enqueue((root, 0));
            }
        }

        while (queue.Count > 0)
        {
            (string id, int currentRank) = queue.Dequeue();
            rank[id] = currentRank;
            foreach (string next in successors[id])
            {
                if (visited.Add(next))
                {
                    queue.Enqueue((next, currentRank + 1));
                }
            }
        }

        return rank;
    }

    private static Dictionary<int, List<string>> BuildRanks(
        IReadOnlyList<NodeSpec> nodes,
        Dictionary<string, int> rank,
        Dictionary<string, int> order)
    {
        Dictionary<int, List<string>> ranks = [];
        foreach (NodeSpec node in nodes)
        {
            int r = rank[node.Id];
            if (!ranks.TryGetValue(r, out List<string>? list))
            {
                list = new List<string>();
                ranks[r] = list;
            }

            list.Add(node.Id);
        }

        foreach (List<string> list in ranks.Values)
        {
            list.Sort((a, b) => order[a].CompareTo(order[b]));
        }

        return ranks;
    }

    private static void OrderRanks(
        Dictionary<int, List<string>> ranks,
        Dictionary<string, List<string>> predecessors,
        Dictionary<string, int> order)
    {
        int maxRank = ranks.Keys.Max();
        Dictionary<string, double> positions = [];

        for (int iteration = 0; iteration < 3; iteration++)
        {
            for (int r = 1; r <= maxRank; r++)
            {
                if (!ranks.TryGetValue(r, out List<string>? list) || !ranks.TryGetValue(r - 1, out List<string>? previous))
                {
                    continue;
                }

                var previousPositions = new Dictionary<string, double>();
                for (int i = 0; i < previous.Count; i++)
                {
                    previousPositions[previous[i]] = i;
                }

                var scored = new List<(string Id, double Score, int Order)>();
                foreach (string id in list)
                {
                    double sum = 0;
                    int count = 0;
                    foreach (string pred in predecessors[id])
                    {
                        if (previousPositions.TryGetValue(pred, out double predPosition))
                        {
                            sum += predPosition;
                            count++;
                        }
                    }

                    double score = count > 0 ? sum / count : (positions.TryGetValue(id, out double existing) ? existing : order[id]);
                    scored.Add((id, score, order[id]));
                }

                scored.Sort((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score) : a.Order.CompareTo(b.Order));
                list.Clear();
                foreach ((string id, _, _) in scored)
                {
                    list.Add(id);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    positions[list[i]] = i;
                }
            }
        }
    }

    private static Point FlowchartLabelAnchor(Rect fromRect, Rect toRect, bool horizontal, bool reverse)
    {
        if (horizontal)
        {
            double midX = reverse
                ? (fromRect.Left + toRect.Right) / 2
                : (fromRect.Right + toRect.Left) / 2;
            double crossMid = (fromRect.Y + fromRect.Height / 2 + toRect.Y + toRect.Height / 2) / 2;
            return new Point(midX, crossMid);
        }

        double centerX = (fromRect.X + fromRect.Width / 2 + toRect.X + toRect.Width / 2) / 2;
        double flowMid = reverse
            ? (fromRect.Top + toRect.Bottom) / 2
            : (fromRect.Bottom + toRect.Top) / 2;
        return new Point(centerX, flowMid);
    }

    private static Point NudgeLabelClear(Point preferred, Size labelSize, IReadOnlyDictionary<string, Rect> bounds)
    {
        double halfW = labelSize.Width / 2 + 10;
        double halfH = labelSize.Height / 2 + 6;
        if (LabelClear(preferred, halfW, halfH, bounds))
        {
            return preferred;
        }

        const double step = 6;
        for (int ring = 1; ring <= 30; ring++)
        {
            double distance = ring * step;
            foreach ((double dx, double dy) in new[]
            {
                (1.0, 0.0), (-1.0, 0.0), (0.0, 1.0), (0.0, -1.0)
            })
            {
                var candidate = new Point(preferred.X + dx * distance, preferred.Y + dy * distance);
                if (LabelClear(candidate, halfW, halfH, bounds))
                {
                    return candidate;
                }
            }
        }

        return preferred;
    }

    private static bool LabelClear(Point center, double halfW, double halfH, IReadOnlyDictionary<string, Rect> bounds)
    {
        foreach (Rect rect in bounds.Values)
        {
            if (center.X + halfW > rect.Left && center.X - halfW < rect.Right &&
                center.Y + halfH > rect.Top && center.Y - halfH < rect.Bottom)
            {
                return false;
            }
        }

        return true;
    }

    private static void DrawFlowchartEdges(
        MermaidDrawingContext context,
        IReadOnlyList<MermaidFlowEdgeDefinition> edges,
        IReadOnlyDictionary<string, Rect> bounds,
        IReadOnlyDictionary<string, int> rank,
        bool horizontal,
        bool reverse)
    {
        foreach (MermaidFlowEdgeDefinition edge in edges)
        {
            if (!bounds.TryGetValue(edge.FromId, out Rect fromRect) || !bounds.TryGetValue(edge.ToId, out Rect toRect))
            {
                continue;
            }

            Point start;
            Point end;
            if (horizontal)
            {
                start = new Point(reverse ? fromRect.Left : fromRect.Right, fromRect.Y + fromRect.Height / 2);
                end = new Point(reverse ? toRect.Right : toRect.Left, toRect.Y + toRect.Height / 2);
            }
            else
            {
                start = new Point(fromRect.X + fromRect.Width / 2, reverse ? fromRect.Top : fromRect.Bottom);
                end = new Point(toRect.X + toRect.Width / 2, reverse ? toRect.Bottom : toRect.Top);
            }

            bool adjacent = rank.TryGetValue(edge.FromId, out int fromRank) &&
                            rank.TryGetValue(edge.ToId, out int toRank) &&
                            toRank == fromRank + 1;

            if (adjacent)
            {
                DrawAdjacentEdge(context, start, end, edge.Dotted, horizontal);
            }
            else
            {
                AddCurvedArrow(context, start, end, edge.Dotted, horizontal);
            }
        }
    }

    private static void DrawMindmapNode(MermaidDrawingContext context, MermaidMindmapNodeDefinition node, IReadOnlyDictionary<string, Rect> bounds)
    {
        DrawNode(context, new NodeSpec(node.Id, node.Label, node.Shape), bounds[node.Id]);
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            DrawMindmapNode(context, child, bounds);
        }
    }

    private static void DrawMindmapEdges(MermaidDrawingContext context, MermaidMindmapNodeDefinition node, IReadOnlyDictionary<string, Rect> bounds)
    {
        if (!bounds.TryGetValue(node.Id, out Rect parentRect))
        {
            return;
        }

        var parentCenter = new Point(parentRect.X + parentRect.Width / 2, parentRect.Y + parentRect.Height / 2);
        var parentHalf = new Size(parentRect.Width / 2, parentRect.Height / 2);

        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            if (!bounds.TryGetValue(child.Id, out Rect childRect))
            {
                continue;
            }

            var childCenter = new Point(childRect.X + childRect.Width / 2, childRect.Y + childRect.Height / 2);
            var childHalf = new Size(childRect.Width / 2, childRect.Height / 2);

            Point start = BoundaryPoint(parentCenter, parentHalf, new Point(childCenter.X - parentCenter.X, childCenter.Y - parentCenter.Y));
            Point end = BoundaryPoint(childCenter, childHalf, new Point(parentCenter.X - childCenter.X, parentCenter.Y - childCenter.Y));

            context.AddLine(start, end, context.Palette.NodeStroke, 1);

            DrawMindmapEdges(context, child, bounds);
        }
    }

    private static Point BoundaryPoint(Point center, Size half, Point direction)
    {
        double dx = direction.X;
        double dy = direction.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-9)
        {
            return center;
        }

        double ux = dx / length;
        double uy = dy / length;
        double tx = ux != 0 ? half.Width / Math.Abs(ux) : double.PositiveInfinity;
        double ty = uy != 0 ? half.Height / Math.Abs(uy) : double.PositiveInfinity;
        double t = Math.Min(tx, ty);
        return new Point(center.X + ux * t, center.Y + uy * t);
    }

    private static (Size Size, Dictionary<string, Rect> Bounds) LayoutMindmapRadial(
        MermaidDrawingContext context,
        MermaidMindmapNodeDefinition root)
    {
        Dictionary<string, Size> sizes = [];
        MeasureMindmapSizes(context, root, sizes);

        Dictionary<string, double> radii = [];
        ComputeMindmapRadii(root, sizes, radii);

        Dictionary<string, double> weights = [];
        CountMindmapLeaves(root, weights);

        Dictionary<string, Rect> bounds = [];
        PlaceMindmapRadial(root, new Point(0, 0), -Math.PI / 2, Math.PI * 2, sizes, radii, weights, bounds);

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (Rect rect in bounds.Values)
        {
            minX = Math.Min(minX, rect.X);
            minY = Math.Min(minY, rect.Y);
            maxX = Math.Max(maxX, rect.X + rect.Width);
            maxY = Math.Max(maxY, rect.Y + rect.Height);
        }

        Dictionary<string, Rect> shifted = [with(bounds.Count)];
        foreach ((string id, Rect rect) in bounds)
        {
            shifted[id] = new Rect(
                rect.X - minX + ContentPadding,
                rect.Y - minY + ContentPadding,
                rect.Width,
                rect.Height);
        }

        return (new Size(maxX - minX + ContentPadding * 2, maxY - minY + ContentPadding * 2), shifted);
    }

    private static double ComputeMindmapRadii(
        MermaidMindmapNodeDefinition node,
        IReadOnlyDictionary<string, Size> sizes,
        Dictionary<string, double> radii)
    {
        Size size = sizes[node.Id];
        double half = Math.Sqrt(
            (size.Width / 2) * (size.Width / 2) +
            (size.Height / 2) * (size.Height / 2));

        double max = half;
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            max = Math.Max(max, ComputeMindmapRadii(child, sizes, radii));
        }

        radii[node.Id] = max;
        return max;
    }

    private static void PlaceMindmapRadial(
        MermaidMindmapNodeDefinition node,
        Point center,
        double angleStart,
        double angleSpan,
        IReadOnlyDictionary<string, Size> sizes,
        IReadOnlyDictionary<string, double> radii,
        IReadOnlyDictionary<string, double> weights,
        Dictionary<string, Rect> bounds)
    {
        Size nodeSize = sizes[node.Id];
        bounds[node.Id] = new Rect(
            center.X - nodeSize.Width / 2,
            center.Y - nodeSize.Height / 2,
            nodeSize.Width,
            nodeSize.Height);

        if (node.Children.Count == 0)
        {
            return;
        }

        double sumWeight = 0;
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            sumWeight += weights[child.Id];
        }

        if (sumWeight <= 0)
        {
            sumWeight = node.Children.Count;
        }

        Size parentSize = sizes[node.Id];
        double parentHalf = Math.Sqrt(
            (parentSize.Width / 2) * (parentSize.Width / 2) +
            (parentSize.Height / 2) * (parentSize.Height / 2));

        var placed = new List<(MermaidMindmapNodeDefinition Child, double Angle, double Distance)>();
        double angle = angleStart;
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            double childSpan = angleSpan * (weights[child.Id] / sumWeight);
            double mid = angle + childSpan / 2;
            double distance = parentHalf + radii[child.Id] + MindmapRingGap;
            placed.Add((child, mid, distance));
            angle += childSpan;
        }

        bool expanded = true;
        int guard = 0;
        while (expanded && guard++ < 24)
        {
            expanded = false;
            for (int i = 0; i < placed.Count; i++)
            {
                for (int j = i + 1; j < placed.Count; j++)
                {
                    (_, double ai, double di) = placed[i];
                    (_, double aj, double dj) = placed[j];
                    double dx = di * Math.Cos(ai) - dj * Math.Cos(aj);
                    double dy = di * Math.Sin(ai) - dj * Math.Sin(aj);
                    double separation = Math.Sqrt(dx * dx + dy * dy);
                    double need = radii[placed[i].Child.Id] + radii[placed[j].Child.Id] + MindmapRingGap;
                    if (separation < need)
                    {
                        double push = (need - separation) / 2;
                        placed[i] = (placed[i].Child, ai, di + push);
                        placed[j] = (placed[j].Child, aj, dj + push);
                        expanded = true;
                    }
                }
            }
        }

        foreach ((MermaidMindmapNodeDefinition child, double mid, double distance) in placed)
        {
            double childSpan = angleSpan * (weights[child.Id] / sumWeight);
            var childCenter = new Point(
                center.X + distance * Math.Cos(mid),
                center.Y + distance * Math.Sin(mid));
            PlaceMindmapRadial(child, childCenter, mid - childSpan / 2, childSpan, sizes, radii, weights, bounds);
        }
    }

    private static void MeasureMindmapSizes(MermaidDrawingContext context, MermaidMindmapNodeDefinition node, Dictionary<string, Size> sizes)
    {
        Size labelSize = context.MeasureText(node.Label);
        double width = labelSize.Width + LabelPadding;
        double height = labelSize.Height + 16;
        if (node.Shape == MermaidNodeShape.Circle)
        {
            width = Math.Max(labelSize.Width + 20, 44);
            height = width;
        }

        sizes[node.Id] = new Size(width, height);
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            MeasureMindmapSizes(context, child, sizes);
        }
    }

    private static double CountMindmapLeaves(MermaidMindmapNodeDefinition node, Dictionary<string, double> leaves)
    {
        if (node.Children.Count == 0)
        {
            leaves[node.Id] = 1;
            return 1;
        }

        double total = 0;
        foreach (MermaidMindmapNodeDefinition child in node.Children)
        {
            total += CountMindmapLeaves(child, leaves);
        }

        leaves[node.Id] = Math.Max(1, total);
        return leaves[node.Id];
    }

    private static void AddPieWedge(
        MermaidDrawingContext context,
        Point center,
        double radius,
        double startDegrees,
        double sweepDegrees,
        Brush fill)
    {
        if (sweepDegrees <= 0)
        {
            return;
        }

        double a1 = (startDegrees - 90) * Math.PI / 180;
        double a2 = (startDegrees + sweepDegrees - 90) * Math.PI / 180;
        var p1 = new Point(center.X + radius * Math.Cos(a1), center.Y + radius * Math.Sin(a1));
        var p2 = new Point(center.X + radius * Math.Cos(a2), center.Y + radius * Math.Sin(a2));

        var figure = new PathFigure { StartPoint = center, IsClosed = true };
        figure.Segments.Add(new LineSegment { Point = p1 });
        figure.Segments.Add(new ArcSegment
        {
            Point = p2,
            Size = new Size(radius, radius),
            IsLargeArc = sweepDegrees > 180,
            SweepDirection = SweepDirection.Clockwise
        });

        var path = new Microsoft.UI.Xaml.Shapes.Path
        {
            Data = new PathGeometry { Figures = { figure } },
            Fill = fill,
            Stroke = context.Palette.Surface,
            StrokeThickness = 1
        };

        Canvas.SetLeft(path, 0);
        Canvas.SetTop(path, 0);
        context.Canvas.Children.Add(path);
    }

    private static Color ToWindowsColor(MermaidColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);
}
