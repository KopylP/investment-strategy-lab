namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class NodeSockets
{
    public const string Cash = "cash";
    public const string Risk = "risk";
    public const string Of = "of";
    public const string Funds = "funds";
    public const string Budget = "budget";
    public const string Remainder = "remainder";
    public const string Asset = "asset";
    public const string Left = "left";
    public const string Right = "right";
    public const string Value = "value";
    public const string Indicator = "indicator";
    public const string Other = "other";
    public const string Conditions = "conditions";
    public const string Condition = "condition";
    public const string RankBy = "rankBy";

    public static SocketType? OutputOf(NodeKind kind) => kind switch
    {
        NodeKind.Indicator => SocketType.Number,
        NodeKind.SelectionResult => SocketType.Funds,
        NodeKind.RiskResult => SocketType.RiskPolicy,
        NodeKind.FundSelection or NodeKind.EntryResult or NodeKind.ExitResult => SocketType.Candidates,
        NodeKind.MarketResult => SocketType.Budget,
        _ when NodeKinds.IsCondition(kind) => SocketType.Boolean,
        _ => null,
    };

    public static SocketType? OutputOf(GraphNode node, string output) => node.Kind == NodeKind.Setup
        ? output switch
        {
            "funds" or "benchmarkId" or "cashId" or "volatilityIndexId" => SocketType.Series,
            "extraSeries" => SocketType.SeriesList,
            _ when output.StartsWith("extra:", StringComparison.Ordinal) && Guid.TryParse(output[6..], out _) => SocketType.Series,
            _ => null,
        }
        : node.Kind == NodeKind.Cash && output == "series" ? SocketType.Series
        : node.Kind == NodeKind.Cash && output == "output" ? SocketType.Budget
        : node.Kind == NodeKind.SelectionResult && output == "cash" ? SocketType.Budget
        : output == "output" ? OutputOf(node.Kind) : null;

    public static IReadOnlyList<InputSocket> InputsOf(NodeKind kind) => kind switch
    {
        NodeKind.Indicator => [new(Of, SocketType.Series, false, false)],
        NodeKind.Compare => [new(Left, SocketType.Number, true, false), new(Right, SocketType.Number, false, false)],
        NodeKind.Between => [new(Value, SocketType.Number, true, false)],
        NodeKind.Crossed => [new(Indicator, SocketType.Number, true, false), new(Other, SocketType.Number, false, false)],
        NodeKind.All or NodeKind.Any => [new(Conditions, SocketType.Boolean, true, true)],
        NodeKind.Not => [new(Condition, SocketType.Boolean, true, false)],
        NodeKind.MarketResult => [new(Condition, SocketType.Boolean, false, false)],
        NodeKind.ExitResult or NodeKind.EntryResult => [new(Funds, SocketType.Candidates, true, false), new(Condition, SocketType.Boolean, false, false)],
        NodeKind.FundSelection => [new(Funds, SocketType.Candidates, true, false), new(RankBy, SocketType.Number, false, false)],
        NodeKind.SelectionResult => [new(Funds, SocketType.Candidates, true, false), new(Budget, SocketType.Budget, true, false)],
        NodeKind.Portfolio => [new(Funds, SocketType.Funds, true, false), new(Cash, SocketType.Budget, true, false), new(Risk, SocketType.RiskPolicy, true, false)],
        NodeKind.Cash => [new(Remainder, SocketType.Budget, true, false)],
        _ => [],
    };
}
