namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySetup(
    Guid StrategyId,
    UserId OwnerId,
    int Version,
    bool CanBeReplayed,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    ReplayBenchmark Benchmark,
    ReplayCadence RebalanceCadence,
    ReplayCadence? RiskReviewCadence,
    ReplaySignalTriggers Signals,
    string Currency,
    IReadOnlyList<string> Comparisons)
{
    public IReadOnlyList<Guid> PricedInstruments
    {
        get
        {
            IEnumerable<Guid> cash = CashInstrumentId is { } id ? [id] : [];
            IEnumerable<Guid> volatilityIndex = Signals.VolatilityIndexInstrumentId is { } index ? [index] : [];

            return [.. Universe.Append(Benchmark.InstrumentId).Concat(cash).Concat(volatilityIndex).Distinct()];
        }
    }
}
