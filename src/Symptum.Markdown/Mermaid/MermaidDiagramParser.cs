using System.Globalization;

namespace Symptum.Markdown.Mermaid;

public static class MermaidDiagramParser
{
    public static MermaidDiagramDefinition Parse(string source)
    {
        var normalized = MermaidSyntax.NormalizeCode(source);
        var lines = MermaidStandardPreprocessor.Preprocess(normalized);

        if (lines.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(normalized, "The Mermaid source is empty.");
        }

        var header = lines[0].Trim();
        if (header.StartsWith("flowchart", StringComparison.OrdinalIgnoreCase) ||
            header.StartsWith("graph", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFlowchart(normalized, lines);
        }

        if (header.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSequenceDiagram(normalized, lines);
        }

        if (header.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase))
        {
            return ParseStateDiagram(normalized, lines);
        }

        if (header.StartsWith("pie", StringComparison.OrdinalIgnoreCase))
        {
            return ParsePieChart(normalized, lines);
        }

        if (header.StartsWith("quadrantChart", StringComparison.OrdinalIgnoreCase))
        {
            return ParseQuadrantChart(normalized, lines);
        }

        if (header.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMindmap(normalized, lines);
        }

        return new MermaidUnsupportedDiagramDefinition(normalized, $"Unsupported Mermaid syntax: {header}.");
    }

    private static MermaidDiagramDefinition ParseFlowchart(string source, IReadOnlyList<string> lines)
    {
        var direction = ParseFlowDirection(lines[0]);
        var nodeBuilders = new Dictionary<string, MermaidFlowNodeBuilder>(StringComparer.Ordinal);
        var nodeOrder = new List<string>();
        var edges = new List<MermaidFlowEdgeDefinition>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var lineSpan = line.AsSpan();
            if (TryParseFlowEdgeChain(lineSpan, nodeBuilders, nodeOrder, edges))
            {
                continue;
            }

            var standaloneIndex = 0;
            if (TryParseFlowNode(lineSpan, ref standaloneIndex, out var node) &&
                ConsumeRemainingWhitespace(lineSpan, standaloneIndex))
            {
                RegisterNode(nodeBuilders, nodeOrder, node);
            }
        }

        if (nodeOrder.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(source, "No flowchart nodes could be parsed from the Mermaid source.");
        }

        var nodes = nodeOrder
            .Select(id => nodeBuilders[id].Build())
            .ToList();

        return new MermaidFlowchartDiagramDefinition(source, direction, nodes, edges);
    }

    private static MermaidDiagramDefinition ParseSequenceDiagram(string source, IReadOnlyList<string> lines)
    {
        var participants = new Dictionary<string, MermaidSequenceParticipantDefinition>(StringComparer.Ordinal);
        var participantOrder = new List<string>();
        var messages = new List<MermaidSequenceMessageDefinition>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryParseParticipant(line, out var participant))
            {
                RegisterParticipant(participants, participantOrder, participant);
                continue;
            }

            if (TryParseMessage(line, out var message))
            {
                RegisterParticipant(participants, participantOrder, new MermaidSequenceParticipantDefinition(message.FromId, message.FromId));
                RegisterParticipant(participants, participantOrder, new MermaidSequenceParticipantDefinition(message.ToId, message.ToId));
                messages.Add(message);
            }
        }

        if (participantOrder.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(source, "No sequence participants could be parsed from the Mermaid source.");
        }

        if (messages.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(source, "No sequence messages could be parsed from the Mermaid source.");
        }

        var orderedParticipants = participantOrder
            .Select(id => participants[id])
            .ToList();

        return new MermaidSequenceDiagramDefinition(source, orderedParticipants, messages);
    }

    private static MermaidDiagramDefinition ParseStateDiagram(string source, IReadOnlyList<string> lines)
    {
        var direction = MermaidFlowDirection.TopToBottom;
        var stateBuilders = new Dictionary<string, MermaidStateNodeBuilder>(StringComparer.Ordinal);
        var stateOrder = new List<string>();
        var transitions = new List<MermaidStateTransitionDefinition>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(line) || line is "{" or "}")
            {
                continue;
            }

            if (TryParseDiagramDirection(line, out var parsedDirection))
            {
                direction = parsedDirection;
                continue;
            }

            if (TryParseStateTransition(line, out var transition, out var fromState, out var toState))
            {
                RegisterStateNode(stateBuilders, stateOrder, fromState);
                RegisterStateNode(stateBuilders, stateOrder, toState);
                transitions.Add(transition);
                continue;
            }

            if (TryParseStateDeclaration(line, out var state))
            {
                RegisterStateNode(stateBuilders, stateOrder, state);
            }
        }

        if (stateOrder.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(source, "No state nodes could be parsed from the Mermaid source.");
        }

        if (transitions.Count == 0)
        {
            return new MermaidUnsupportedDiagramDefinition(source, "No state transitions could be parsed from the Mermaid source.");
        }

        var states = stateOrder
            .Select(id => stateBuilders[id].Build())
            .ToList();

        return new MermaidStateDiagramDefinition(source, direction, states, transitions);
    }

    private static MermaidDiagramDefinition ParsePieChart(string source, IReadOnlyList<string> lines)
    {
        var header = lines[0].Trim();
        var showData = header.Contains("showData", StringComparison.OrdinalIgnoreCase);
        var slices = new List<MermaidPieSliceDefinition>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("title ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParsePieSlice(line, out var slice))
            {
                slices.Add(slice);
            }
        }

        return slices.Count == 0
            ? new MermaidUnsupportedDiagramDefinition(source, "No pie chart slices could be parsed from the Mermaid source.")
            : new MermaidPieDiagramDefinition(source, showData, slices);
    }

    private static MermaidDiagramDefinition ParseQuadrantChart(string source, IReadOnlyList<string> lines)
    {
        var xLeftLabel = "Low";
        var xRightLabel = "High";
        var yBottomLabel = "Low";
        var yTopLabel = "High";
        var quadrantLabels = new string[4];
        var points = new List<MermaidQuadrantPointDefinition>();

        for (var index = 1; index < lines.Count; index++)
        {
            var line = StripComment(lines[index]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("title ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("x-axis ", StringComparison.OrdinalIgnoreCase))
            {
                ParseAxisDescriptor(line["x-axis ".Length..], out xLeftLabel, out xRightLabel);
                continue;
            }

            if (line.StartsWith("y-axis ", StringComparison.OrdinalIgnoreCase))
            {
                ParseAxisDescriptor(line["y-axis ".Length..], out yBottomLabel, out yTopLabel);
                continue;
            }

            if (TryParseQuadrantLabel(line, out var quadrantIndex, out var quadrantLabel))
            {
                quadrantLabels[quadrantIndex] = quadrantLabel;
                continue;
            }

            if (TryParseQuadrantPoint(line, out var point))
            {
                points.Add(point);
            }
        }

        var hasQuadrantMetadata = quadrantLabels.Any(static label => !string.IsNullOrWhiteSpace(label));
        return !hasQuadrantMetadata && points.Count == 0
            ? new MermaidUnsupportedDiagramDefinition(source, "No quadrant chart labels or points could be parsed from the Mermaid source.")
            : new MermaidQuadrantChartDiagramDefinition(
                source,
                xLeftLabel,
                xRightLabel,
                yBottomLabel,
                yTopLabel,
                quadrantLabels,
                points);
    }

    private static MermaidDiagramDefinition ParseMindmap(string source, IReadOnlyList<string> lines)
    {
        MermaidMindmapNodeBuilder? root = null;
        var stack = new Stack<MermaidMindmapNodeContext>();
        var nextNodeIndex = 0;

        for (var index = 1; index < lines.Count; index++)
        {
            var rawLine = lines[index];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var trimmedLine = rawLine.Trim();
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            var indent = CountLeadingIndent(rawLine);
            var node = CreateMindmapNode(trimmedLine, nextNodeIndex++);
            if (root is null)
            {
                root = node;
                stack.Push(new MermaidMindmapNodeContext(indent, node));
                continue;
            }

            while (stack.Count > 0 && indent <= stack.Peek().Indent)
            {
                stack.Pop();
            }

            var parent = stack.Count > 0 ? stack.Peek().Node : root;
            parent.Children.Add(node);
            stack.Push(new MermaidMindmapNodeContext(indent, node));
        }

        return root is null
            ? new MermaidUnsupportedDiagramDefinition(source, "No mind map nodes could be parsed from the Mermaid source.")
            : new MermaidMindmapDiagramDefinition(source, root.Build(), CountMindmapNodes(root));
    }

    private static bool TryParseDiagramDirection(string line, out MermaidFlowDirection direction)
    {
        direction = MermaidFlowDirection.TopToBottom;
        if (!line.StartsWith("direction ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        direction = ParseFlowDirection(line);
        return true;
    }

    private static MermaidFlowDirection ParseFlowDirection(string header)
    {
        var tokens = header.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return MermaidFlowDirection.TopToBottom;
        }

        return tokens[^1].ToUpperInvariant() switch
        {
            "LR" => MermaidFlowDirection.LeftToRight,
            "RL" => MermaidFlowDirection.RightToLeft,
            "BT" => MermaidFlowDirection.BottomToTop,
            _ => MermaidFlowDirection.TopToBottom
        };
    }

    private static bool TryParseStateDeclaration(string line, out MermaidStateNodeDefinition state)
    {
        state = new MermaidStateNodeDefinition(string.Empty, string.Empty, MermaidNodeShape.Rounded);
        if (!line.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var descriptor = line["state ".Length..].Trim();
        if (descriptor.Length == 0)
        {
            return false;
        }

        descriptor = descriptor.TrimEnd('{').Trim();
        if (descriptor.Length == 0 || descriptor == "[*]")
        {
            return false;
        }

        var shape = descriptor.Contains("<<choice>>", StringComparison.OrdinalIgnoreCase)
            ? MermaidNodeShape.Diamond
            : MermaidNodeShape.Rounded;
        descriptor = descriptor.Replace("<<choice>>", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        state = ParseStateReference(descriptor, isSource: false, shapeOverride: shape);
        return !string.IsNullOrWhiteSpace(state.Id);
    }

    private static bool TryParseStateTransition(
        string line,
        out MermaidStateTransitionDefinition transition,
        out MermaidStateNodeDefinition fromState,
        out MermaidStateNodeDefinition toState)
    {
        transition = new MermaidStateTransitionDefinition(string.Empty, string.Empty, null, Dotted: false);
        fromState = new MermaidStateNodeDefinition(string.Empty, string.Empty, MermaidNodeShape.Rounded);
        toState = new MermaidStateNodeDefinition(string.Empty, string.Empty, MermaidNodeShape.Rounded);

        var colonIndex = line.IndexOf(':');
        var relation = colonIndex >= 0 ? line[..colonIndex].Trim() : line.Trim();
        var label = colonIndex >= 0 ? CleanLabel(line[(colonIndex + 1)..].Trim()) : null;
        if (relation.Length == 0)
        {
            return false;
        }

        foreach (var arrow in new[] { "..>", "-->", "->" })
        {
            var arrowIndex = relation.IndexOf(arrow, StringComparison.Ordinal);
            if (arrowIndex < 0)
            {
                continue;
            }

            var fromToken = relation[..arrowIndex].Trim();
            var toToken = relation[(arrowIndex + arrow.Length)..].Trim();
            if (fromToken.Length == 0 || toToken.Length == 0)
            {
                return false;
            }

            fromState = ParseStateReference(fromToken, isSource: true);
            toState = ParseStateReference(toToken, isSource: false);
            transition = new MermaidStateTransitionDefinition(fromState.Id, toState.Id, label, Dotted: arrow.Contains("..", StringComparison.Ordinal));
            return true;
        }

        return false;
    }

    private static MermaidStateNodeDefinition ParseStateReference(
        string token,
        bool isSource,
        MermaidNodeShape? shapeOverride = null)
    {
        var descriptor = token.Trim();
        if (descriptor == "[*]")
        {
            return isSource
                ? new MermaidStateNodeDefinition("__state_start", string.Empty, MermaidNodeShape.Circle)
                : new MermaidStateNodeDefinition("__state_end", string.Empty, MermaidNodeShape.Circle);
        }

        if (descriptor.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
        {
            descriptor = descriptor["state ".Length..].Trim();
        }

        descriptor = descriptor.TrimEnd('{').Trim();
        var id = descriptor;
        var label = descriptor;

        var asIndex = descriptor.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
        if (asIndex >= 0)
        {
            var left = descriptor[..asIndex].Trim();
            var right = descriptor[(asIndex + 4)..].Trim();
            if (left.StartsWith('"') && left.EndsWith('"'))
            {
                id = right;
                label = CleanLabel(left);
            }
            else
            {
                id = left;
                label = CleanLabel(right);
            }
        }

        label = CleanLabel(label);
        id = CleanDiagramIdentifier(id);
        if (label.Length == 0)
        {
            label = id;
        }

        return new MermaidStateNodeDefinition(id, label, shapeOverride ?? MermaidNodeShape.Rounded);
    }

    private static void RegisterStateNode(
        Dictionary<string, MermaidStateNodeBuilder> stateBuilders,
        List<string> stateOrder,
        MermaidStateNodeDefinition state)
    {
        if (!stateBuilders.TryGetValue(state.Id, out var builder))
        {
            builder = new MermaidStateNodeBuilder(state.Id, state.Label, state.Shape);
            stateBuilders.Add(state.Id, builder);
            stateOrder.Add(state.Id);
            return;
        }

        if (!string.IsNullOrWhiteSpace(state.Label))
        {
            builder.Label = state.Label;
        }

        builder.Shape = state.Shape;
    }

    private static bool TryParsePieSlice(string line, out MermaidPieSliceDefinition slice)
    {
        slice = new MermaidPieSliceDefinition(string.Empty, 0);
        var colonIndex = line.IndexOf(':');
        if (colonIndex <= 0 || colonIndex >= line.Length - 1)
        {
            return false;
        }

        var label = CleanLabel(line[..colonIndex].Trim());
        var valueText = line[(colonIndex + 1)..].Trim();
        if (label.Length == 0 ||
            !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            value <= 0)
        {
            return false;
        }

        slice = new MermaidPieSliceDefinition(label, value);
        return true;
    }

    private static void ParseAxisDescriptor(string descriptor, out string firstLabel, out string secondLabel)
    {
        var arrowIndex = descriptor.IndexOf("-->", StringComparison.Ordinal);
        if (arrowIndex < 0)
        {
            firstLabel = CleanLabel(descriptor.Trim());
            secondLabel = string.Empty;
            return;
        }

        firstLabel = CleanLabel(descriptor[..arrowIndex].Trim());
        secondLabel = CleanLabel(descriptor[(arrowIndex + 3)..].Trim());
    }

    private static bool TryParseQuadrantLabel(string line, out int quadrantIndex, out string label)
    {
        quadrantIndex = -1;
        label = string.Empty;
        if (!line.StartsWith("quadrant-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var separatorIndex = line.IndexOf(' ');
        if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
        {
            return false;
        }

        if (!int.TryParse(line["quadrant-".Length..separatorIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex) ||
            parsedIndex is < 1 or > 4)
        {
            return false;
        }

        quadrantIndex = parsedIndex - 1;
        label = CleanLabel(line[(separatorIndex + 1)..].Trim());
        return label.Length > 0;
    }

    private static bool TryParseQuadrantPoint(string line, out MermaidQuadrantPointDefinition point)
    {
        point = new MermaidQuadrantPointDefinition(string.Empty, 0, 0, 6, 1.5, null, null);
        var bracketStart = line.IndexOf('[');
        var bracketEnd = line.IndexOf(']', bracketStart >= 0 ? bracketStart + 1 : 0);
        if (bracketStart <= 0 || bracketEnd <= bracketStart)
        {
            return false;
        }

        var label = StripDiagramClassifier(line[..bracketStart].Trim().TrimEnd(':').Trim());
        label = CleanLabel(label);
        if (label.Length == 0)
        {
            return false;
        }

        var coordinates = line[(bracketStart + 1)..bracketEnd]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (coordinates.Length != 2 ||
            !double.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            x < 0 || x > 1 || y < 0 || y > 1)
        {
            return false;
        }

        var radius = 6d;
        var strokeWidth = 1.5d;
        MermaidColor? fillColor = null;
        MermaidColor? strokeColor = null;
        var styleText = line[(bracketEnd + 1)..].Trim();
        if (styleText.Length > 0)
        {
            var styleParts = styleText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var stylePart in styleParts)
            {
                var separatorIndex = stylePart.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex >= stylePart.Length - 1)
                {
                    continue;
                }

                var key = stylePart[..separatorIndex].Trim();
                var value = stylePart[(separatorIndex + 1)..].Trim();
                switch (key.ToLowerInvariant())
                {
                    case "radius" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRadius):
                        radius = Math.Clamp(parsedRadius, 3, 24);
                        break;
                    case "stroke-width" when TryParseDimension(value, out var parsedStrokeWidth):
                        strokeWidth = Math.Clamp(parsedStrokeWidth, 0.5, 8);
                        break;
                    case "color" when TryParseHexColor(value, out var parsedFill):
                        fillColor = parsedFill;
                        break;
                    case "stroke-color" when TryParseHexColor(value, out var parsedStroke):
                        strokeColor = parsedStroke;
                        break;
                }
            }
        }

        point = new MermaidQuadrantPointDefinition(label, x, y, radius, strokeWidth, fillColor, strokeColor);
        return true;
    }

    private static MermaidMindmapNodeBuilder CreateMindmapNode(string line, int nodeIndex)
    {
        var descriptor = StripMindmapMetadata(line);
        var descriptorSpan = descriptor.AsSpan();
        var index = 0;
        if (TryParseFlowNode(descriptorSpan, ref index, out var parsedNode) &&
            ConsumeRemainingWhitespace(descriptorSpan, index))
        {
            return new MermaidMindmapNodeBuilder($"mindmap-{nodeIndex}-{parsedNode.Id}", parsedNode.Label, parsedNode.Shape);
        }

        var label = CleanLabel(descriptor);
        return new MermaidMindmapNodeBuilder($"mindmap-{nodeIndex}", label.Length == 0 ? $"Node {nodeIndex + 1}" : label, MermaidNodeShape.Rounded);
    }

    private static string StripMindmapMetadata(string line)
    {
        var withoutClasses = StripDiagramClassifier(line);
        var iconIndex = withoutClasses.IndexOf("::icon(", StringComparison.OrdinalIgnoreCase);
        return (iconIndex >= 0 ? withoutClasses[..iconIndex] : withoutClasses).Trim();
    }

    private static int CountMindmapNodes(MermaidMindmapNodeBuilder builder)
    {
        var count = 1;
        foreach (var child in builder.Children)
        {
            count += CountMindmapNodes(child);
        }

        return count;
    }

    private static bool TryParseDimension(string value, out double dimension)
    {
        var normalized = value.Trim();
        if (normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].TrimEnd();
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out dimension);
    }

    private static bool TryParseHexColor(string value, out MermaidColor color)
    {
        color = default;
        var normalized = value.Trim();
        if (!normalized.StartsWith('#') || (normalized.Length != 7 && normalized.Length != 9))
        {
            return false;
        }

        if (!uint.TryParse(normalized[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        color = normalized.Length == 7
            ? MermaidColor.FromRgb((byte)(parsed >> 16), (byte)(parsed >> 8), (byte)parsed)
            : MermaidColor.FromArgb((byte)(parsed >> 24), (byte)(parsed >> 16), (byte)(parsed >> 8), (byte)parsed);
        return true;
    }

    private static string StripComment(string line)
    {
        var commentIndex = line.IndexOf("%%", StringComparison.Ordinal);
        var content = commentIndex >= 0 ? line[..commentIndex] : line;
        return content.Trim();
    }

    private static bool TryParseFlowEdgeChain(
        ReadOnlySpan<char> line,
        Dictionary<string, MermaidFlowNodeBuilder> nodeBuilders,
        List<string> nodeOrder,
        List<MermaidFlowEdgeDefinition> edges)
    {
        var index = 0;
        if (!TryParseFlowNode(line, ref index, out var currentNode))
        {
            return false;
        }

        var hadEdge = false;
        RegisterNode(nodeBuilders, nodeOrder, currentNode);

        while (true)
        {
            SkipWhitespace(line, ref index);
            if (index >= line.Length)
            {
                break;
            }

            if (!TryParseFlowConnector(line, ref index, out var edgeLabel, out var dotted))
            {
                return false;
            }

            SkipWhitespace(line, ref index);
            if (!TryParseFlowNode(line, ref index, out var nextNode))
            {
                return false;
            }

            RegisterNode(nodeBuilders, nodeOrder, nextNode);
            edges.Add(new MermaidFlowEdgeDefinition(currentNode.Id, nextNode.Id, edgeLabel, dotted));
            currentNode = nextNode;
            hadEdge = true;
        }

        return hadEdge;
    }

    private static bool TryParseFlowNode(ReadOnlySpan<char> line, ref int index, out MermaidParsedFlowNode node)
    {
        SkipWhitespace(line, ref index);
        node = default;
        if (index >= line.Length)
        {
            return false;
        }

        var idStart = index;
        while (index < line.Length && IsNodeIdentifierChar(line[index]))
        {
            index++;
        }

        if (index == idStart)
        {
            return false;
        }

        var id = line[idStart..index].ToString();
        var label = id;
        var shape = MermaidNodeShape.Rounded;
        var hasExplicitLabel = false;

        if (index < line.Length)
        {
            if (line[index] == '[' && TryParseDelimited(line, ref index, '[', ']', out var rectangleLabel))
            {
                label = rectangleLabel;
                shape = MermaidNodeShape.Rectangle;
                hasExplicitLabel = true;
            }
            else if (line[index] == '{' && TryParseDelimited(line, ref index, '{', '}', out var diamondLabel))
            {
                label = diamondLabel;
                shape = MermaidNodeShape.Diamond;
                hasExplicitLabel = true;
            }
            else if (line[index] == '(' && index + 1 < line.Length && line[index + 1] == '(' && TryParseDoubleParen(line, ref index, out var circleLabel))
            {
                label = circleLabel;
                shape = MermaidNodeShape.Circle;
                hasExplicitLabel = true;
            }
            else if (line[index] == '(' && TryParseDelimited(line, ref index, '(', ')', out var roundedLabel))
            {
                label = roundedLabel;
                shape = MermaidNodeShape.Rounded;
                hasExplicitLabel = true;
            }
        }

        node = new MermaidParsedFlowNode(id, CleanLabel(label), shape, hasExplicitLabel);
        return true;
    }

    private static bool TryParseDelimited(ReadOnlySpan<char> line, ref int index, char open, char close, out string content)
    {
        content = string.Empty;
        if (index >= line.Length || line[index] != open)
        {
            return false;
        }

        index++;
        var start = index;
        while (index < line.Length && line[index] != close)
        {
            index++;
        }

        if (index >= line.Length)
        {
            return false;
        }

        content = line[start..index].ToString();
        index++;
        return true;
    }

    private static bool TryParseDoubleParen(ReadOnlySpan<char> line, ref int index, out string content)
    {
        content = string.Empty;
        if (index + 1 >= line.Length || line[index] != '(' || line[index + 1] != '(')
        {
            return false;
        }

        index += 2;
        var start = index;
        while (index + 1 < line.Length && !(line[index] == ')' && line[index + 1] == ')'))
        {
            index++;
        }

        if (index + 1 >= line.Length)
        {
            return false;
        }

        content = line[start..index].ToString();
        index += 2;
        return true;
    }

    private static bool TryParseFlowConnector(ReadOnlySpan<char> line, ref int index, out string? label, out bool dotted)
    {
        label = null;
        dotted = false;
        SkipWhitespace(line, ref index);

        var sawArrowHead = false;
        var sawConnectorContent = false;

        while (index < line.Length)
        {
            var ch = line[index];
            if (ch == '|')
            {
                index++;
                var labelStart = index;
                while (index < line.Length && line[index] != '|')
                {
                    index++;
                }

                label = line[labelStart..Math.Min(index, line.Length)].ToString().Trim();
                if (index < line.Length && line[index] == '|')
                {
                    index++;
                }

                sawConnectorContent = true;
                continue;
            }

            if (ch == '>')
            {
                sawArrowHead = true;
                index++;
                continue;
            }

            if (ch is '-' or '=' or '.')
            {
                dotted |= ch == '.';
                sawConnectorContent = true;
                index++;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                index++;
                continue;
            }

            break;
        }

        return sawArrowHead && sawConnectorContent;
    }

    private static void RegisterNode(
        Dictionary<string, MermaidFlowNodeBuilder> nodeBuilders,
        List<string> nodeOrder,
        MermaidParsedFlowNode parsedNode)
    {
        if (!nodeBuilders.TryGetValue(parsedNode.Id, out var builder))
        {
            builder = new MermaidFlowNodeBuilder(parsedNode.Id, parsedNode.Label, parsedNode.Shape);
            nodeBuilders.Add(parsedNode.Id, builder);
            nodeOrder.Add(parsedNode.Id);
            return;
        }

        if (parsedNode.HasExplicitLabel)
        {
            builder.Label = parsedNode.Label;
            builder.Shape = parsedNode.Shape;
        }
    }

    private static bool ConsumeRemainingWhitespace(ReadOnlySpan<char> line, int index)
    {
        while (index < line.Length)
        {
            if (!char.IsWhiteSpace(line[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    private static bool TryParseParticipant(string line, out MermaidSequenceParticipantDefinition participant)
    {
        participant = new MermaidSequenceParticipantDefinition(string.Empty, string.Empty);

        var trimmed = line.Trim();
        var prefix = trimmed.StartsWith("participant ", StringComparison.OrdinalIgnoreCase)
            ? "participant "
            : trimmed.StartsWith("actor ", StringComparison.OrdinalIgnoreCase)
                ? "actor "
                : null;
        if (prefix is null)
        {
            return false;
        }

        var descriptor = trimmed[prefix.Length..].Trim();
        if (descriptor.Length == 0)
        {
            return false;
        }

        var asIndex = descriptor.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
        if (asIndex >= 0)
        {
            var id = descriptor[..asIndex].Trim();
            var label = descriptor[(asIndex + 4)..].Trim();
            if (id.Length == 0 || label.Length == 0)
            {
                return false;
            }

            participant = new MermaidSequenceParticipantDefinition(id, CleanLabel(label));
            return true;
        }

        participant = new MermaidSequenceParticipantDefinition(descriptor, CleanLabel(descriptor));
        return true;
    }

    private static bool TryParseMessage(string line, out MermaidSequenceMessageDefinition message)
    {
        message = new MermaidSequenceMessageDefinition(string.Empty, string.Empty, string.Empty, Dotted: false, Emphasized: false);
        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0)
        {
            return false;
        }

        var relation = line[..colonIndex].Trim();
        var label = CleanLabel(line[(colonIndex + 1)..].Trim());
        if (relation.Length == 0 || label.Length == 0)
        {
            return false;
        }

        foreach (var arrow in new[] { "-->>", "->>", "-->", "->" })
        {
            var arrowIndex = relation.IndexOf(arrow, StringComparison.Ordinal);
            if (arrowIndex < 0)
            {
                continue;
            }

            var from = relation[..arrowIndex].Trim();
            var to = relation[(arrowIndex + arrow.Length)..].Trim();
            if (from.Length == 0 || to.Length == 0)
            {
                return false;
            }

            message = new MermaidSequenceMessageDefinition(
                from,
                to,
                label,
                Dotted: arrow.Contains("--", StringComparison.Ordinal),
                Emphasized: arrow.EndsWith(">>", StringComparison.Ordinal));
            return true;
        }

        return false;
    }

    private static void RegisterParticipant(
        Dictionary<string, MermaidSequenceParticipantDefinition> participants,
        List<string> participantOrder,
        MermaidSequenceParticipantDefinition participant)
    {
        if (participants.ContainsKey(participant.Id))
        {
            return;
        }

        participants.Add(participant.Id, participant);
        participantOrder.Add(participant.Id);
    }

    private static void SkipWhitespace(ReadOnlySpan<char> line, ref int index)
    {
        while (index < line.Length && char.IsWhiteSpace(line[index]))
        {
            index++;
        }
    }

    private static bool IsNodeIdentifierChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.';
    }

    private static string CleanLabel(string value)
    {
        return value.Trim().Trim('"');
    }

    private static string CleanDiagramIdentifier(string value)
    {
        var cleaned = StripDiagramClassifier(value.Trim());
        if (cleaned.StartsWith("class ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["class ".Length..].Trim();
        }

        if (cleaned.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["state ".Length..].Trim();
        }

        cleaned = cleaned.Trim().Trim('"');
        if (cleaned.EndsWith("{", StringComparison.Ordinal))
        {
            cleaned = cleaned[..^1].TrimEnd();
        }

        var bracketIndex = cleaned.IndexOf('[');
        if (bracketIndex > 0)
        {
            cleaned = cleaned[..bracketIndex].TrimEnd();
        }

        return cleaned;
    }

    private static string StripDiagramClassifier(string value)
    {
        var classifierIndex = value.IndexOf(":::", StringComparison.Ordinal);
        return classifierIndex >= 0 ? value[..classifierIndex].TrimEnd() : value;
    }

    private static int CountLeadingIndent(string line)
    {
        var indent = 0;
        foreach (var ch in line)
        {
            if (ch == ' ')
            {
                indent++;
                continue;
            }

            if (ch == '\t')
            {
                indent += 4;
                continue;
            }

            break;
        }

        return indent;
    }

    private sealed class MermaidFlowNodeBuilder(string id, string label, MermaidNodeShape shape)
    {
        public string Id { get; } = id;

        public string Label { get; set; } = label;

        public MermaidNodeShape Shape { get; set; } = shape;

        public MermaidFlowNodeDefinition Build() => new(Id, Label, Shape);
    }

    private sealed class MermaidStateNodeBuilder(string id, string label, MermaidNodeShape shape)
    {
        public string Id { get; } = id;

        public string Label { get; set; } = label;

        public MermaidNodeShape Shape { get; set; } = shape;

        public MermaidStateNodeDefinition Build() => new(Id, Label, Shape);
    }

    private sealed class MermaidMindmapNodeBuilder(string id, string label, MermaidNodeShape shape)
    {
        public string Id { get; } = id;

        public string Label { get; } = label;

        public MermaidNodeShape Shape { get; } = shape;

        public List<MermaidMindmapNodeBuilder> Children { get; } = [];

        public MermaidMindmapNodeDefinition Build()
        {
            return new MermaidMindmapNodeDefinition(
                Id,
                Label,
                Shape,
                Children.Select(static child => child.Build()).ToList());
        }
    }

    private readonly record struct MermaidParsedFlowNode(string Id, string Label, MermaidNodeShape Shape, bool HasExplicitLabel);

    private readonly record struct MermaidMindmapNodeContext(int Indent, MermaidMindmapNodeBuilder Node);
}
