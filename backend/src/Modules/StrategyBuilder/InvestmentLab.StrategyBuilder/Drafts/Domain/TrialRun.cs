using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record TrialRun(
    int Number,
    Guid StrategyId,
    int StrategyVersion,
    string Fingerprint,
    string? Label,
    DateTimeOffset RanAt,
    Guid? ExperimentId = null,
    TrialExperiment? Experiment = null);
