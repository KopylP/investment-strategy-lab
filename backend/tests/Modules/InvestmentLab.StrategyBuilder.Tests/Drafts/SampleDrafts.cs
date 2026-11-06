using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

internal static class SampleDrafts
{
    public static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
    public static readonly Guid FundA = Guid.Parse("0199f000-0000-7000-8000-00000000000a");
    public static readonly Guid FundB = Guid.Parse("0199f000-0000-7000-8000-00000000000b");
    public static readonly Guid Benchmark = Guid.Parse("0199f000-0000-7000-8000-0000000000be");
    public static readonly Guid Cash = Guid.Parse("0199f000-0000-7000-8000-0000000000ca");
    public static readonly Guid Gold = Guid.Parse("0199f000-0000-7000-8000-0000000000a1");

    public static DraftSetup Setup => new([FundA, FundB], Benchmark, Cash, null, [Gold]);

    public static StrategyDraft Of(StrategyGraph graph, DraftSetup? setup = null) =>
        StrategyDraft.Start(UserId.New(), "Sample", setup ?? Setup, graph, [], null, Now).Value;

    public static GraphNode Indicator(string id, BlockKind block, string kind, string series = "fund", int? days = null, int? months = null)
    {
        var parameters = NodeParameters.Empty.With("indicator", kind).WithObject("of", new { series });
        if (days is { } dayCount)
            parameters = parameters.With("days", dayCount);
        if (months is { } monthCount)
            parameters = parameters.With("months", monthCount);

        return new GraphNode(new NodeId(id), NodeKind.Indicator, block, parameters);
    }

    public static GraphNode Compare(string id, BlockKind block, string op, double? value = null)
    {
        var parameters = NodeParameters.Empty.With("operator", op);
        if (value is { } number)
            parameters = parameters.With("value", number);

        return new GraphNode(new NodeId(id), NodeKind.Compare, block, parameters);
    }

    public static GraphNode Logic(string id, BlockKind block, NodeKind kind) => new(new NodeId(id), kind, block, NodeParameters.Empty);

    public static GraphLink Link(string from, string to, string input) => new(new NodeId(from), new NodeId(to), input);

    public static StrategyGraph Graph(IEnumerable<GraphNode> nodes, IEnumerable<GraphLink> links, NodeParameters? market = null)
    {
        var graph = StrategyGraph.Empty with { Nodes = [.. StrategyGraph.Empty.Nodes, .. nodes], Links = [.. StrategyGraph.Empty.Links, .. links] };

        return market is null ? graph : graph.WithNode(graph.ResultOf(BlockKind.MarketFilter) with { Parameters = market });
    }

    public static StrategyGraph MomentumEntry() =>
        Graph(
            [
                Indicator("mom", BlockKind.Entry, "blendedMomentum"),
                Compare("positive", BlockKind.Entry, "gt", 0),
            ],
            [
                Link("mom", "positive", "left"),
                Link("positive", "result-entry", "condition"),
            ]);

    public static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;
}
