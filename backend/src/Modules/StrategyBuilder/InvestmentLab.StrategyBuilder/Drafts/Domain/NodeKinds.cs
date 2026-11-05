namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class NodeKinds
{
    public static bool IsResult(NodeKind kind) =>
        kind is NodeKind.MarketResult or NodeKind.ExitResult or NodeKind.EntryResult or NodeKind.SelectionResult or NodeKind.RiskResult;

    public static BlockKind ResultBlock(NodeKind kind) => kind switch
    {
        NodeKind.MarketResult => BlockKind.MarketFilter,
        NodeKind.ExitResult => BlockKind.Exit,
        NodeKind.EntryResult => BlockKind.Entry,
        NodeKind.SelectionResult => BlockKind.Selection,
        NodeKind.RiskResult => BlockKind.Risk,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static bool IsCondition(NodeKind kind) =>
        kind is NodeKind.Compare or NodeKind.Between or NodeKind.Crossed or NodeKind.Signal or NodeKind.All or NodeKind.Any or NodeKind.Not;

    public static string ConditionCode(NodeKind kind) => kind switch
    {
        NodeKind.Compare => "compare",
        NodeKind.Between => "between",
        NodeKind.Crossed => "crossed",
        NodeKind.Signal => "signal",
        NodeKind.All => "all",
        NodeKind.Any => "any",
        NodeKind.Not => "not",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a condition."),
    };

    public static NodeKind OfConditionCode(string code) => code switch
    {
        "compare" => NodeKind.Compare,
        "between" => NodeKind.Between,
        "crossed" => NodeKind.Crossed,
        "signal" => NodeKind.Signal,
        "all" => NodeKind.All,
        "any" => NodeKind.Any,
        "not" => NodeKind.Not,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Not a condition."),
    };
}
