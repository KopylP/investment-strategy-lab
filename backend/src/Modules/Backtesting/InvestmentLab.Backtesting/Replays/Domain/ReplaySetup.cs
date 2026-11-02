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
    ReplaySignalTriggers Signals,
    string Currency)
{
    public IReadOnlyList<Guid> PricedInstruments
    {
        get
        {
            IEnumerable<Guid> cash = CashInstrumentId is { } id ? [id] : [];
            IEnumerable<Guid> volatilityIndex = Signals.VolatilityIndexInstrumentId is { } index ? [index] : [];

            return [.. Universe.Append(BenchmarkInstrumentId).Concat(cash).Concat(volatilityIndex).Distinct()];
        }
    }
}
