namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record GuardrailLimits(Percentage? MaxPositionWeight, Percentage? MaxMonthlyTurnover)
{
    public static GuardrailLimits None => new(null, null);
}
