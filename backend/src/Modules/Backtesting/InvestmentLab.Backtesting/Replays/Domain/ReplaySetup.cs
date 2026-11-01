namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySetup(
    Guid StrategyId,
    UserId OwnerId,
    int Version,
    bool CanBeReplayed,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    Guid BenchmarkInstrumentId,
    ReplayCadence RebalanceCadence,
    ReplayCadence? RiskReviewCadence,
    string Currency)
{
    public IReadOnlyList<Guid> PricedInstruments
    {
        get
        {
            IEnumerable<Guid> cash = CashInstrumentId is { } id ? [id] : [];

            return [.. Universe.Append(BenchmarkInstrumentId).Concat(cash).Distinct()];
        }
    }
}
