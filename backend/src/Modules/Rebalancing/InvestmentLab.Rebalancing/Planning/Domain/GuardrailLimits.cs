namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record GuardrailLimits(Percentage? MaxPositionWeight, Percentage? MaxMonthlyTurnover)
{
    public static GuardrailLimits None => new(null, null);

    public static GuardrailLimits Of(IPlanningSubPortfolio sub) => new(
        sub.MaxPositionWeight is { } weight ? Percentage.Of(weight) : null,
        sub.MaxMonthlyTurnover is { } turnover ? Percentage.Of(turnover) : null);
}
