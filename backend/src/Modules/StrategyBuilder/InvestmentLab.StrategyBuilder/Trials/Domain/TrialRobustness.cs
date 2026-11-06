namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialRobustness(
    DateOnly? OutOfSampleStart = null,
    TrialWindows? WalkForward = null,
    TrialParameterNeighborhood? Neighborhood = null);
