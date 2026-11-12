namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialExperiment(
    TrialPeriod Period,
    decimal InitialCapital,
    TrialExecutionCosts Costs,
    TrialRobustness Robustness);
