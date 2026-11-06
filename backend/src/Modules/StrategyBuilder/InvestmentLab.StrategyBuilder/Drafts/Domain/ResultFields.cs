namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class ResultFields
{
    public const string InvestWhenUnfavorable = "investWhenUnfavorable";

    public const string WhenUnfavorable = "whenUnfavorable";

    public const string ReduceHoldings = "reduceHoldings";

    public static readonly string[] Market = [InvestWhenUnfavorable, WhenUnfavorable];

    public static readonly string[] Selection = ["order", "top", "maxPerGroup", "maxCorrelation", "correlationDays"];

    public static readonly string[] Weights = ["method", "volatilityDays", "maxPerFund", "targetVolatility", "targetVolatilityDays"];

    public static readonly string[] Risk = ["stopLossFromCost", "drawdownFromPeak"];

    public static IReadOnlyList<string> Of(NodeKind kind) => kind switch
    {
        NodeKind.MarketResult => Market,
        NodeKind.SelectionResult => Weights,
        NodeKind.FundSelection => Selection,
        NodeKind.RiskResult => Risk,
        _ => [],
    };
}
