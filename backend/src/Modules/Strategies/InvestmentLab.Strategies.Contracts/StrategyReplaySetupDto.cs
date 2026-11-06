namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyReplaySetupDto(
    Guid StrategyId,
    Guid OwnerId,
    int Version,
    bool SupportsBacktest,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    Guid BenchmarkInstrumentId,
    IReadOnlyList<StrategyReportDto> Reports,
    IReadOnlyList<string> Comparisons,
    IReadOnlyList<Guid>? LogicSignalIds = null,
    int HistoryBars = 260,
    IReadOnlyList<Guid>? ExtraSeries = null);
