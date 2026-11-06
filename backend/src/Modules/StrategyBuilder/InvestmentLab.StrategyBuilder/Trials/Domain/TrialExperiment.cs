namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialExperiment(
    TrialPeriod Period,
    decimal InitialCapital,
    TrialExecutionCosts Costs,
    TrialRobustness Robustness)
{
    public Error? ShapeProblem()
    {
        if (Period is null || Costs is null || Robustness is null)
            return TrialErrors.ExperimentConditionsMissing();

        if (HasIncompleteNeighborhood())
            return TrialErrors.ExperimentConditionsMissing();

        return null;
    }

    private bool HasIncompleteNeighborhood() =>
        Robustness.Neighborhood is { } neighborhood && (neighborhood.Path is null || neighborhood.Values is null);
}
