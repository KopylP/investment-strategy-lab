using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ResearchReport(
    ReplayResult Baseline,
    IReadOnlyList<ResearchStudy> Studies,
    string ParametersHash,
    string EngineVersion,
    string SelectionMetric = "sharpe",
    string ReplayModel = "daily-close/v2",
    IReadOnlyDictionary<Guid, string>? InstrumentLabels = null,
    Guid? CashInstrumentId = null);
