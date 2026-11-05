using InvestmentLab.StrategyBuilder.Drafts.Domain;
using static InvestmentLab.StrategyBuilder.Tests.Drafts.SampleDrafts;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class ExitFromEntryTests
{
    [Fact]
    public void A_positive_momentum_entry_becomes_a_negative_momentum_exit_with_the_margin()
    {
        var exit = ExitFromEntry.Of(MomentumEntry(), EngineSchema.Vocabulary, 0.02);

        var compare = ExitCondition(exit, NodeKind.Compare);

        compare.Parameters.Text("operator").ShouldBe("lt");
        compare.Parameters.Number("value")!.Value.ShouldBe(-0.02, tolerance: 0.000001);
        Input(exit, compare, NodeSockets.Left).Parameters.Text("indicator").ShouldBe("blendedMomentum");
    }

    [Fact]
    public void All_becomes_any_of_the_negations()
    {
        var graph = Graph(
            [
                Indicator("momentum", BlockKind.Entry, "momentum"),
                Indicator("trend", BlockKind.Entry, "priceVsSma", days: 200),
                Compare("positive", BlockKind.Entry, "gt", 0),
                Compare("above-trend", BlockKind.Entry, "gt", 0),
                Logic("both", BlockKind.Entry, NodeKind.All),
            ],
            [
                Link("momentum", "positive", NodeSockets.Left),
                Link("trend", "above-trend", NodeSockets.Left),
                Link("positive", "both", NodeSockets.Conditions),
                Link("above-trend", "both", NodeSockets.Conditions),
                Link("both", StrategyGraph.EntryResultId.Value, NodeSockets.Condition),
            ]);

        var exit = ExitFromEntry.Of(graph, EngineSchema.Vocabulary, 0.02);
        var any = ExitCondition(exit, NodeKind.Any);
        var children = exit.Into(any.Id, NodeSockets.Conditions).Select(link => exit.Node(link.From)!).ToList();

        children.Count.ShouldBe(2);
        children.ShouldAllBe(node => node.Kind == NodeKind.Compare);
        children.ShouldAllBe(node => node.Parameters.Text("operator") == "lt");
    }

    [Fact]
    public void A_comparison_of_two_indicators_flips_without_a_margin()
    {
        var graph = Graph(
            [Indicator("price", BlockKind.Entry, "price"), Indicator("average", BlockKind.Entry, "sma", days: 200), Compare("above", BlockKind.Entry, "gt")],
            [Link("price", "above", NodeSockets.Left), Link("average", "above", NodeSockets.Right), Link("above", StrategyGraph.EntryResultId.Value, NodeSockets.Condition)]);

        var exit = ExitFromEntry.Of(graph, EngineSchema.Vocabulary, 0.02);
        var compare = ExitCondition(exit, NodeKind.Compare);

        compare.Parameters.Text("operator").ShouldBe("lt");
        compare.Parameters.Number(NodeSockets.Value).ShouldBeNull();
        exit.Into(compare.Id, NodeSockets.Right).ShouldHaveSingleItem();
    }

    [Fact]
    public void A_price_level_gets_no_margin()
    {
        var graph = Graph(
            [Indicator("price", BlockKind.Entry, "price"), Compare("above", BlockKind.Entry, "gt", 100)],
            [Link("price", "above", NodeSockets.Left), Link("above", StrategyGraph.EntryResultId.Value, NodeSockets.Condition)]);

        var exit = ExitFromEntry.Of(graph, EngineSchema.Vocabulary, 0.02);

        ExitCondition(exit, NodeKind.Compare).Parameters.Number(NodeSockets.Value).ShouldBe(100);
    }

    [Fact]
    public void A_signal_condition_flips_its_state()
    {
        var signal = new GraphNode(new NodeId("signal"), NodeKind.Signal, BlockKind.Entry,
            NodeParameters.Empty.With("signalId", Guid.CreateVersion7().ToString()).With("state", "active").With("scope", "thisFund"));
        var graph = Graph([signal], [Link("signal", StrategyGraph.EntryResultId.Value, NodeSockets.Condition)]);

        var exit = ExitFromEntry.Of(graph, EngineSchema.Vocabulary, 0.02);

        ExitCondition(exit, NodeKind.Signal).Parameters.Text("state").ShouldBe("inactive");
    }

    [Fact]
    public void Not_is_removed_instead_of_doubled()
    {
        var signal = new GraphNode(new NodeId("signal"), NodeKind.Signal, BlockKind.Entry,
            NodeParameters.Empty.With("signalId", Guid.CreateVersion7().ToString()).With("state", "active").With("scope", "thisFund"));
        var graph = Graph(
            [signal, Logic("not", BlockKind.Entry, NodeKind.Not)],
            [Link("signal", "not", NodeSockets.Condition), Link("not", StrategyGraph.EntryResultId.Value, NodeSockets.Condition)]);

        var exit = ExitFromEntry.Of(graph, EngineSchema.Vocabulary, 0.02);

        ExitCondition(exit, NodeKind.Signal).Parameters.Text("state").ShouldBe("active");
        exit.Nodes.ShouldNotContain(node => node.Block == BlockKind.Exit && node.Kind == NodeKind.Not);
    }

    [Fact]
    public void Replacing_exit_keeps_shared_entry_nodes_and_reuses_the_indicator()
    {
        var source = MomentumEntry();
        source = source with { Links = [.. source.Links, Link("positive", "result-exit", "condition")] };
        var result = ExitFromEntry.Of(source, EngineSchema.Vocabulary, 0.02);
        result.Nodes.Count(node => node.Kind == NodeKind.Indicator).ShouldBe(1);
        result.Node(new NodeId("positive")).ShouldNotBeNull();
        result.Into(StrategyGraph.EntryResultId, "condition").ShouldHaveSingleItem().From.Value.ShouldBe("positive");
        Input(result, ExitCondition(result, NodeKind.Compare), NodeSockets.Left).Id.Value.ShouldBe("mom");
        GraphCheck.Of(Of(result), EngineSchema.Vocabulary).ShouldBeEmpty();
    }

    private static GraphNode ExitCondition(StrategyGraph graph, NodeKind expectedKind)
    {
        var link = graph.Into(StrategyGraph.ExitResultId, NodeSockets.Condition).ShouldHaveSingleItem();
        var condition = graph.Node(link.From).ShouldNotBeNull();

        condition.Kind.ShouldBe(expectedKind);

        return condition;
    }

    private static GraphNode Input(StrategyGraph graph, GraphNode node, string socket)
    {
        var link = graph.Into(node.Id, socket).ShouldHaveSingleItem();

        return graph.Node(link.From).ShouldNotBeNull();
    }
}
