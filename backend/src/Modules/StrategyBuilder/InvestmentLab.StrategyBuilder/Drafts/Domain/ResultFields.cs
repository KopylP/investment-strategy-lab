namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class ResultFields
{
    public const string InvestWhenUnfavorable = "investWhenUnfavorable";

    public static readonly string[] Selection = ["order", "top", "maxPerGroup", "maxCorrelation", "correlationDays"];

    public static readonly string[] Weights = ["method", "volatilityDays", "maxPerFund", "targetVolatility", "targetVolatilityDays"];

    public static readonly string[] Risk = ["stopLossFromCost", "drawdownFromPeak"];

    public static IReadOnlyList<string> Of(NodeKind kind) => kind switch
    {
        NodeKind.MarketResult => [InvestWhenUnfavorable],
        NodeKind.SelectionResult => [.. Selection, .. Weights],
        NodeKind.RiskResult => Risk,
        _ => [],
    };
}
