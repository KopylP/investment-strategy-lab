namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record UnfinishedExperiment(Guid Id, ExperimentStatus Status);
