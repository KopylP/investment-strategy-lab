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
    IReadOnlyList<string> Comparisons,
    IReadOnlyList<Guid>? LogicSignalIds = null,
    IReadOnlyList<Guid>? SignalInstruments = null,
    int HistoryBars = 260,
    IReadOnlyList<Guid>? ExtraSeries = null,
    ReplaySchedule? RebalanceSchedule = null,
    ReplaySchedule? RiskReviewSchedule = null,
    string? UnsupportedSchedule = null,
    ReplayExperiment? Experiment = null,
    string? ParametersOverride = null,
    IReadOnlyDictionary<Guid, string>? InstrumentLabels = null)
{
    public IReadOnlyList<Guid> PricedInstruments
    {
        get
        {
            IEnumerable<Guid> cash = CashInstrumentId is { } id ? [id] : [];
            IEnumerable<Guid> volatilityIndex = Signals.VolatilityIndexInstrumentId is { } index ? [index] : [];

            return [.. Universe.Append(Benchmark.InstrumentId).Concat(cash).Concat(volatilityIndex).Concat(SignalInstruments ?? []).Concat(ExtraSeries ?? []).Distinct()];
        }
    }
}
