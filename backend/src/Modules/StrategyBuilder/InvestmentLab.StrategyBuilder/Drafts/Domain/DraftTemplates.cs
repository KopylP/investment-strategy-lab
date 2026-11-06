namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class DraftTemplates
{
    public const string MomentumTrendFilter = "momentum-trend-filter";
    public const string DualMomentum = "dual-momentum";
    public const string IndexHold = "index-hold";

    public static readonly string[] Codes = [MomentumTrendFilter, DualMomentum, IndexHold];

    public static StrategyGraph GraphOf(string code) => PortfolioFlow.Upgrade(SetupBindings.WithLegacyBindings(code switch
    {
        MomentumTrendFilter => MomentumWithTrendFilter(),
        DualMomentum => DualMomentumGraph(),
        IndexHold => IndexHoldGraph(),
        _ => StrategyGraph.Empty,
    }, force: true));

    public static bool IsKnown(string code) => Codes.Contains(code);

    private static StrategyGraph MomentumWithTrendFilter()
    {
        var builder = new TemplateBuilder();

        builder.Indicator("m-price", BlockKind.MarketFilter, "price", "benchmark");
        builder.Indicator("m-sma", BlockKind.MarketFilter, "sma", "benchmark", ("days", 200));
        builder.Compare("m-above", BlockKind.MarketFilter, "gt", "m-price", right: "m-sma");
        builder.Link("m-above", StrategyGraph.MarketResultId.Value, NodeSockets.Condition);
        builder.Result(StrategyGraph.MarketResultId, ("investWhenUnfavorable", 0.3));

        builder.Indicator("e-mom", BlockKind.Entry, "blendedMomentum", "fund");
        builder.Indicator("e-trend", BlockKind.Entry, "priceVsSma", "fund", ("days", 200));
        builder.Compare("e-mom-positive", BlockKind.Entry, "gt", "e-mom", value: 0);
        builder.Compare("e-trend-positive", BlockKind.Entry, "gt", "e-trend", value: 0);
        builder.Logic("e-all", BlockKind.Entry, NodeKind.All, "e-mom-positive", "e-trend-positive");
        builder.Link("e-all", StrategyGraph.EntryResultId.Value, NodeSockets.Condition);

        builder.Indicator("x-trend", BlockKind.Exit, "priceVsSma", "fund", ("days", 200));
        builder.Crossed("x-cross", BlockKind.Exit, "x-trend", "down", -0.03, 21);
        builder.Link("x-cross", StrategyGraph.ExitResultId.Value, NodeSockets.Condition);

        builder.Indicator("s-mom", BlockKind.Selection, "blendedMomentum", "fund");
        builder.Link("s-mom", StrategyGraph.SelectionResultId.Value, NodeSockets.RankBy);
        builder.Result(
            StrategyGraph.SelectionResultId,
            ("top", 3), ("order", "desc"), ("maxPerGroup", 1), ("maxCorrelation", 0.7), ("correlationDays", 126),
            ("method", "inverseVolatility"), ("volatilityDays", 63), ("maxPerFund", 0.4), ("targetVolatility", 0.14), ("targetVolatilityDays", 21));

        return builder.Graph;
    }

    private static StrategyGraph DualMomentumGraph()
    {
        var builder = new TemplateBuilder();

        builder.Indicator("e-mom", BlockKind.Entry, "momentum", "fund", ("months", 12));
        builder.Indicator("e-rs", BlockKind.Entry, "relativeStrength", "fund", ("months", 12));
        builder.Compare("e-mom-positive", BlockKind.Entry, "gt", "e-mom", value: 0);
        builder.Compare("e-rs-positive", BlockKind.Entry, "gt", "e-rs", value: 0);
        builder.Logic("e-all", BlockKind.Entry, NodeKind.All, "e-mom-positive", "e-rs-positive");
        builder.Link("e-all", StrategyGraph.EntryResultId.Value, NodeSockets.Condition);

        builder.Indicator("x-mom", BlockKind.Exit, "momentum", "fund", ("months", 12));
        builder.Indicator("x-rs", BlockKind.Exit, "relativeStrength", "fund", ("months", 12));
        builder.Compare("x-mom-positive", BlockKind.Exit, "gt", "x-mom", value: 0);
        builder.Compare("x-rs-positive", BlockKind.Exit, "gt", "x-rs", value: 0);
        builder.Logic("x-all", BlockKind.Exit, NodeKind.All, "x-mom-positive", "x-rs-positive");
        builder.Logic("x-not", BlockKind.Exit, NodeKind.Not);
        builder.Link("x-all", "x-not", NodeSockets.Condition);
        builder.Link("x-not", StrategyGraph.ExitResultId.Value, NodeSockets.Condition);

        builder.Indicator("s-mom", BlockKind.Selection, "momentum", "fund", ("months", 12));
        builder.Link("s-mom", StrategyGraph.SelectionResultId.Value, NodeSockets.RankBy);
        builder.Result(StrategyGraph.SelectionResultId, ("top", 1), ("order", "desc"), ("method", "equal"));

        return builder.Graph;
    }

    private static StrategyGraph IndexHoldGraph()
    {
        var builder = new TemplateBuilder();

        builder.Result(StrategyGraph.SelectionResultId, ("top", 60), ("order", "desc"), ("method", "equal"));

        return builder.Graph;
    }

    private sealed class TemplateBuilder
    {
        public StrategyGraph Graph { get; private set; } = StrategyGraph.Empty;

        public void Indicator(string id, BlockKind block, string kind, string series, params (string Name, int Value)[] numbers)
        {
            var parameters = NodeParameters.Empty.With("indicator", kind).WithObject("of", new { series });
            foreach (var (name, value) in numbers)
                parameters = parameters.With(name, value);

            Add(new GraphNode(new NodeId(id), NodeKind.Indicator, block, parameters));
        }

        public void Compare(string id, BlockKind block, string op, string left, string? right = null, double? value = null)
        {
            var parameters = NodeParameters.Empty.With("operator", op);
            if (value is { } number)
                parameters = parameters.With(NodeSockets.Value, number);

            Add(new GraphNode(new NodeId(id), NodeKind.Compare, block, parameters));
            Link(left, id, NodeSockets.Left);
            if (right is not null)
                Link(right, id, NodeSockets.Right);
        }

        public void Crossed(string id, BlockKind block, string indicator, string direction, double level, int withinDays)
        {
            var parameters = NodeParameters.Empty.With("direction", direction).With("level", level).With("withinDays", withinDays);

            Add(new GraphNode(new NodeId(id), NodeKind.Crossed, block, parameters));
            Link(indicator, id, NodeSockets.Indicator);
        }

        public void Logic(string id, BlockKind block, NodeKind kind, params string[] conditions)
        {
            Add(new GraphNode(new NodeId(id), kind, block, NodeParameters.Empty));
            foreach (var condition in conditions)
                Link(condition, id, NodeSockets.Conditions);
        }

        public void Result(NodeId id, params (string Name, object Value)[] fields)
        {
            var parameters = NodeParameters.Empty;
            foreach (var (name, value) in fields)
                parameters = value is string text ? parameters.With(name, text) : parameters.With(name, Convert.ToDouble(value));

            Graph = Graph.WithNode(Graph.Node(id)! with { Parameters = parameters });
        }

        public void Link(string from, string to, string input) =>
            Graph = Graph with { Links = [.. Graph.Links, new GraphLink(new NodeId(from), new NodeId(to), input)] };

        private void Add(GraphNode node) => Graph = Graph with { Nodes = [.. Graph.Nodes, node] };
    }
}
