namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalTriggers(
    IReadOnlyList<string> RebalanceKinds,
    IReadOnlyList<string> RiskReviewKinds,
    Guid? VolatilityIndexInstrumentId,
    IReadOnlyList<Guid>? RebalanceSignalIds = null,
    IReadOnlyList<Guid>? RiskReviewSignalIds = null)
{
    public static readonly ReplaySignalTriggers None = new([], [], null);

    public IReadOnlyList<Guid> AllSignalIds => [.. (RebalanceSignalIds ?? []).Concat(RiskReviewSignalIds ?? []).Distinct()];

    public IReadOnlyList<string> AllKinds => [.. RebalanceKinds.Concat(RiskReviewKinds).Distinct()];
}
