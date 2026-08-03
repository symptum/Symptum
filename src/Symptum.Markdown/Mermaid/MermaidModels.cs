namespace Symptum.Markdown.Mermaid;

public readonly record struct MermaidColor(byte A, byte R, byte G, byte B)
{
    public static MermaidColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    public static MermaidColor FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);
}

public enum MermaidDiagramKind
{
    Unsupported,
    Flowchart,
    SequenceDiagram,
    StateDiagram,
    PieChart,
    QuadrantChart,
    Mindmap
}

public abstract class MermaidDiagramDefinition(MermaidDiagramKind kind, string source)
{
    public MermaidDiagramKind Kind { get; } = kind;

    public string Source { get; } = source;
}

public sealed class MermaidUnsupportedDiagramDefinition(string source, string reason) : MermaidDiagramDefinition(MermaidDiagramKind.Unsupported, source)
{
    public string Reason { get; } = reason;
}

public enum MermaidFlowDirection
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop
}

public enum MermaidNodeShape
{
    Rounded,
    Rectangle,
    Diamond,
    Circle
}

public sealed class MermaidFlowchartDiagramDefinition(
    string source,
    MermaidFlowDirection direction,
    IReadOnlyList<MermaidFlowNodeDefinition> nodes,
    IReadOnlyList<MermaidFlowEdgeDefinition> edges) : MermaidDiagramDefinition(MermaidDiagramKind.Flowchart, source)
{
    public MermaidFlowDirection Direction { get; } = direction;

    public IReadOnlyList<MermaidFlowNodeDefinition> Nodes { get; } = nodes;

    public IReadOnlyList<MermaidFlowEdgeDefinition> Edges { get; } = edges;
}

public sealed record MermaidFlowNodeDefinition(string Id, string Label, MermaidNodeShape Shape);

public sealed record MermaidFlowEdgeDefinition(string FromId, string ToId, string? Label, bool Dotted);

public sealed class MermaidSequenceDiagramDefinition(
    string source,
    IReadOnlyList<MermaidSequenceParticipantDefinition> participants,
    IReadOnlyList<MermaidSequenceMessageDefinition> messages) : MermaidDiagramDefinition(MermaidDiagramKind.SequenceDiagram, source)
{
    public IReadOnlyList<MermaidSequenceParticipantDefinition> Participants { get; } = participants;

    public IReadOnlyList<MermaidSequenceMessageDefinition> Messages { get; } = messages;
}

public sealed record MermaidSequenceParticipantDefinition(string Id, string Label);

public sealed record MermaidSequenceMessageDefinition(string FromId, string ToId, string Label, bool Dotted, bool Emphasized);

public sealed class MermaidStateDiagramDefinition(
    string source,
    MermaidFlowDirection direction,
    IReadOnlyList<MermaidStateNodeDefinition> states,
    IReadOnlyList<MermaidStateTransitionDefinition> transitions) : MermaidDiagramDefinition(MermaidDiagramKind.StateDiagram, source)
{
    public MermaidFlowDirection Direction { get; } = direction;

    public IReadOnlyList<MermaidStateNodeDefinition> States { get; } = states;

    public IReadOnlyList<MermaidStateTransitionDefinition> Transitions { get; } = transitions;
}

public sealed record MermaidStateNodeDefinition(string Id, string Label, MermaidNodeShape Shape);

public sealed record MermaidStateTransitionDefinition(string FromId, string ToId, string? Label, bool Dotted);

public sealed class MermaidPieDiagramDefinition(
    string source,
    bool showData,
    IReadOnlyList<MermaidPieSliceDefinition> slices) : MermaidDiagramDefinition(MermaidDiagramKind.PieChart, source)
{
    public bool ShowData { get; } = showData;

    public IReadOnlyList<MermaidPieSliceDefinition> Slices { get; } = slices;
}

public sealed record MermaidPieSliceDefinition(string Label, double Value);

public sealed class MermaidQuadrantChartDiagramDefinition(
    string source,
    string xLeftLabel,
    string xRightLabel,
    string yBottomLabel,
    string yTopLabel,
    IReadOnlyList<string> quadrantLabels,
    IReadOnlyList<MermaidQuadrantPointDefinition> points) : MermaidDiagramDefinition(MermaidDiagramKind.QuadrantChart, source)
{
    public string XLeftLabel { get; } = xLeftLabel;

    public string XRightLabel { get; } = xRightLabel;

    public string YBottomLabel { get; } = yBottomLabel;

    public string YTopLabel { get; } = yTopLabel;

    public IReadOnlyList<string> QuadrantLabels { get; } = quadrantLabels;

    public IReadOnlyList<MermaidQuadrantPointDefinition> Points { get; } = points;
}

public sealed record MermaidQuadrantPointDefinition(
    string Label,
    double X,
    double Y,
    double Radius,
    double StrokeWidth,
    MermaidColor? FillColor,
    MermaidColor? StrokeColor);

public sealed class MermaidMindmapDiagramDefinition(
    string source,
    MermaidMindmapNodeDefinition root,
    int nodeCount) : MermaidDiagramDefinition(MermaidDiagramKind.Mindmap, source)
{
    public MermaidMindmapNodeDefinition Root { get; } = root;

    public int NodeCount { get; } = nodeCount;
}

public sealed record MermaidMindmapNodeDefinition(
    string Id,
    string Label,
    MermaidNodeShape Shape,
    IReadOnlyList<MermaidMindmapNodeDefinition> Children);
