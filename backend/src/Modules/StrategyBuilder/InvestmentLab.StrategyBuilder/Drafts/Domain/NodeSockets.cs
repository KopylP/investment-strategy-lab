namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class NodeSockets
{
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
        _ when NodeKinds.IsCondition(kind) => SocketType.Boolean,
        _ => null,
    };

    public static IReadOnlyList<InputSocket> InputsOf(NodeKind kind) => kind switch
    {
        NodeKind.Compare => [new(Left, SocketType.Number, true, false), new(Right, SocketType.Number, false, false)],
        NodeKind.Between => [new(Value, SocketType.Number, true, false)],
        NodeKind.Crossed => [new(Indicator, SocketType.Number, true, false), new(Other, SocketType.Number, false, false)],
        NodeKind.All or NodeKind.Any => [new(Conditions, SocketType.Boolean, true, true)],
        NodeKind.Not => [new(Condition, SocketType.Boolean, true, false)],
        NodeKind.MarketResult or NodeKind.ExitResult or NodeKind.EntryResult => [new(Condition, SocketType.Boolean, false, false)],
        NodeKind.SelectionResult => [new(RankBy, SocketType.Number, false, false)],
        _ => [],
    };
}
