namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalTriggers(
    IReadOnlyList<string> RebalanceKinds,
    IReadOnlyList<string> RiskReviewKinds,
    Guid? VolatilityIndexInstrumentId)
{
    public static readonly ReplaySignalTriggers None = new([], [], null);

    public IReadOnlyList<string> AllKinds => [.. RebalanceKinds.Concat(RiskReviewKinds).Distinct()];
}
