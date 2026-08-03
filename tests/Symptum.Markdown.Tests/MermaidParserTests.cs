using Markdig.Syntax;
using Symptum.Markdown.Mermaid;

namespace Symptum.Markdown.Tests;

[TestClass]
public sealed class MermaidParserTests
{
    [TestMethod]
    public void Pipeline_MermaidFence_ProducesMermaidDiagramBlock()
    {
        const string markdown = """
# Title

```mermaid
flowchart LR
    A[Start] --> B[End]
```

Plain text.
""";

        MarkdownDocument document = Markdig.Markdown.Parse(markdown, MarkdownManager.Pipeline);
        MermaidDiagramBlock? block = document.Descendants<MermaidDiagramBlock>().FirstOrDefault();

        Assert.IsNotNull(block);
        Assert.AreEqual("mermaid", block!.NormalizedInfo);
    }

    [TestMethod]
    public void Pipeline_MermaidAliasFence_ProducesMermaidDiagramBlock()
    {
        const string markdown = "```mmd\nsequenceDiagram\n    A->>B: Hello\n```";

        MarkdownDocument document = Markdig.Markdown.Parse(markdown, MarkdownManager.Pipeline);
        MermaidDiagramBlock? block = document.Descendants<MermaidDiagramBlock>().FirstOrDefault();

        Assert.IsNotNull(block);
    }

    [TestMethod]
    public void Pipeline_NonMermaidFence_IsRegularCodeBlock()
    {
        const string markdown = "```csharp\nvar x = 1;\n```";

        MarkdownDocument document = Markdig.Markdown.Parse(markdown, MarkdownManager.Pipeline);
        MermaidDiagramBlock? mermaid = document.Descendants<MermaidDiagramBlock>().FirstOrDefault();
        FencedCodeBlock? code = document.Descendants<FencedCodeBlock>().FirstOrDefault();

        Assert.IsNull(mermaid);
        Assert.IsNotNull(code);
        Assert.IsInstanceOfType<FencedCodeBlock>(code);
    }
    [TestMethod]
    public void Parse_EmptySource_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse(string.Empty);
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        Assert.IsInstanceOfType<MermaidUnsupportedDiagramDefinition>(result);
    }

    [TestMethod]
    public void Parse_UnknownHeader_ReturnsUnsupportedWithReason()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("gantt\n    todayMarker off");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        var unsupported = (MermaidUnsupportedDiagramDefinition)result;
        Assert.AreEqual("Unsupported Mermaid syntax: gantt.", unsupported.Reason);
    }

    [TestMethod]
    public void Parse_FlowchartLR_ParsesNodesEdgesAndDirection()
    {
        const string source = """
flowchart LR
    A[Start] --> B{Decision}
    B -->|yes| C((End))
""";

        var diagram = (MermaidFlowchartDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.Flowchart, diagram.Kind);
        Assert.AreEqual(MermaidFlowDirection.LeftToRight, diagram.Direction);
        Assert.AreEqual(3, diagram.Nodes.Count);
        Assert.AreEqual(2, diagram.Edges.Count);

        Assert.AreEqual("A", diagram.Nodes[0].Id);
        Assert.AreEqual("Start", diagram.Nodes[0].Label);
        Assert.AreEqual(MermaidNodeShape.Rectangle, diagram.Nodes[0].Shape);

        Assert.AreEqual("B", diagram.Nodes[1].Id);
        Assert.AreEqual("Decision", diagram.Nodes[1].Label);
        Assert.AreEqual(MermaidNodeShape.Diamond, diagram.Nodes[1].Shape);

        Assert.AreEqual("C", diagram.Nodes[2].Id);
        Assert.AreEqual("End", diagram.Nodes[2].Label);
        Assert.AreEqual(MermaidNodeShape.Circle, diagram.Nodes[2].Shape);

        Assert.AreEqual("A", diagram.Edges[0].FromId);
        Assert.AreEqual("B", diagram.Edges[0].ToId);
        Assert.IsFalse(diagram.Edges[0].Dotted);

        Assert.AreEqual("B", diagram.Edges[1].FromId);
        Assert.AreEqual("C", diagram.Edges[1].ToId);
        Assert.AreEqual("yes", diagram.Edges[1].Label);
        Assert.IsFalse(diagram.Edges[1].Dotted);
    }

    [TestMethod]
    public void Parse_FlowchartTB_DottedEdge()
    {
        const string source = """
flowchart TB
    A[Start] -.-> B[Process]
""";

        var diagram = (MermaidFlowchartDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidFlowDirection.TopToBottom, diagram.Direction);
        Assert.IsTrue(diagram.Edges[0].Dotted);
        Assert.AreEqual("B", diagram.Edges[0].ToId);
    }

    [TestMethod]
    public void Parse_Flowchart_NoNodes_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("flowchart LR");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
    }

    [TestMethod]
    public void Parse_SequenceDiagram_ParsesParticipantsAndMessages()
    {
        const string source = """
sequenceDiagram
    Alice->>John: Hello John, how are you?
    John-->>Alice: Great!
""";

        var diagram = (MermaidSequenceDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.SequenceDiagram, diagram.Kind);
        Assert.AreEqual(2, diagram.Participants.Count);
        Assert.AreEqual("Alice", diagram.Participants[0].Id);
        Assert.AreEqual("John", diagram.Participants[1].Id);

        Assert.AreEqual(2, diagram.Messages.Count);
        Assert.AreEqual("Alice", diagram.Messages[0].FromId);
        Assert.AreEqual("John", diagram.Messages[0].ToId);
        Assert.AreEqual("Hello John, how are you?", diagram.Messages[0].Label);
        Assert.IsFalse(diagram.Messages[0].Dotted);
        Assert.IsTrue(diagram.Messages[0].Emphasized);

        Assert.AreEqual("John", diagram.Messages[1].FromId);
        Assert.AreEqual("Alice", diagram.Messages[1].ToId);
        Assert.AreEqual("Great!", diagram.Messages[1].Label);
        Assert.IsTrue(diagram.Messages[1].Dotted);
    }

    [TestMethod]
    public void Parse_StateDiagram_ParsesStatesAndTransitions()
    {
        const string source = """
stateDiagram-v2
    [*] --> Still
    Still --> [*]
    Still --> Moving
    Moving --> Still
    Moving --> Crash
    Crash --> [*]
""";

        var diagram = (MermaidStateDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.StateDiagram, diagram.Kind);
        Assert.AreEqual(MermaidFlowDirection.TopToBottom, diagram.Direction);
        Assert.IsTrue(diagram.States.Count >= 3);
        Assert.IsTrue(diagram.Transitions.Count >= 5);
        Assert.AreEqual("__state_start", diagram.Transitions[0].FromId);
        Assert.AreEqual("Still", diagram.Transitions[0].ToId);
        Assert.AreEqual("Still", diagram.Transitions[1].FromId);
        Assert.AreEqual("__state_end", diagram.Transitions[1].ToId);
    }

    [TestMethod]
    public void Parse_ClassDiagram_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("classDiagram\n    Animal <|-- Duck");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        Assert.IsInstanceOfType<MermaidUnsupportedDiagramDefinition>(result);
    }

    [TestMethod]
    public void Parse_PieChart_ParsesSlices()
    {
        const string source = """
pie title Pets adopted by volunteers
    "Dogs" : 386
    "Cats" : 85
    "Rats" : 15
""";

        var diagram = (MermaidPieDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.PieChart, diagram.Kind);
        Assert.AreEqual(3, diagram.Slices.Count);
        Assert.AreEqual("Dogs", diagram.Slices[0].Label);
        Assert.AreEqual(386, diagram.Slices[0].Value);
        Assert.AreEqual("Cats", diagram.Slices[1].Label);
        Assert.AreEqual(85, diagram.Slices[1].Value);
    }

    [TestMethod]
    public void Parse_UserJourney_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("journey\n    title My working day\n    Make tea: 5: Me");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        Assert.IsInstanceOfType<MermaidUnsupportedDiagramDefinition>(result);
    }

    [TestMethod]
    public void Parse_Timeline_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("timeline\n    title Evolution of Internet\n    1969 : ARPANET");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        Assert.IsInstanceOfType<MermaidUnsupportedDiagramDefinition>(result);
    }

    [TestMethod]
    public void Parse_QuadrantChart_ParsesAxesAndPoints()
    {
        const string source = """
quadrantChart
    title Reach and engagement of campaigns
    x-axis Low Reach --> High Reach
    y-axis Low Engagement --> High Engagement
    quadrant-1 We should expand
    quadrant-2 Need to promote
    quadrant-3 Re-evaluate
    quadrant-4 May be improved
    Campaign A: [0.3, 0.6]
    Campaign B: [0.45, 0.23]
""";

        var diagram = (MermaidQuadrantChartDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.QuadrantChart, diagram.Kind);
        Assert.AreEqual("Low Reach", diagram.XLeftLabel);
        Assert.AreEqual("High Reach", diagram.XRightLabel);
        Assert.AreEqual(4, diagram.QuadrantLabels.Count);
        Assert.AreEqual(2, diagram.Points.Count);
        Assert.AreEqual("Campaign A", diagram.Points[0].Label);
        Assert.AreEqual(0.3, diagram.Points[0].X, 0.001);
        Assert.AreEqual(0.6, diagram.Points[0].Y, 0.001);
    }

    [TestMethod]
    public void Parse_Mindmap_ParsesRootAndChildren()
    {
        const string source = """
mindmap
  root((root))
    node1
      child1
      child2
    node2
""";

        var diagram = (MermaidMindmapDiagramDefinition)MermaidDiagramParser.Parse(source);

        Assert.AreEqual(MermaidDiagramKind.Mindmap, diagram.Kind);
        Assert.AreEqual(5, diagram.NodeCount);
        Assert.AreEqual("root", diagram.Root.Label);
        Assert.AreEqual(MermaidNodeShape.Circle, diagram.Root.Shape);
        Assert.AreEqual(2, diagram.Root.Children.Count);
        Assert.AreEqual("node1", diagram.Root.Children[0].Label);
        Assert.AreEqual(2, diagram.Root.Children[0].Children.Count);
    }

    [TestMethod]
    public void Parse_ErDiagram_ReturnsUnsupported()
    {
        MermaidDiagramDefinition result = MermaidDiagramParser.Parse("erDiagram\n    CUSTOMER ||--o{ ORDER : places");
        Assert.AreEqual(MermaidDiagramKind.Unsupported, result.Kind);
        Assert.IsInstanceOfType<MermaidUnsupportedDiagramDefinition>(result);
    }
}
