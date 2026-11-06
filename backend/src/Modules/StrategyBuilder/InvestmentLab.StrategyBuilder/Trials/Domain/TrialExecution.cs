using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialExecution(TrialVersion Version, Guid? ExperimentId = null, TrialExperiment? Experiment = null);
