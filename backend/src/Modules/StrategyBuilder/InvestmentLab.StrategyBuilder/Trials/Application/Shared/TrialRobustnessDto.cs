using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialRobustnessDto(
    DateOnly? OutOfSampleStart = null,
    TrialWindowsDto? WalkForward = null,
    TrialParameterNeighborhoodDto? Neighborhood = null)
{
    public static TrialRobustnessDto From(TrialRobustness robustness) => new(
        OutOfSampleStart: robustness.OutOfSampleStart,
        WalkForward: robustness.WalkForward is { } windows ? TrialWindowsDto.From(windows) : null,
        Neighborhood: robustness.Neighborhood is { } neighborhood ? TrialParameterNeighborhoodDto.From(neighborhood) : null);

    public Result<TrialRobustness> ToDomain()
    {
        if (Neighborhood is { Path: null } or { Values: null })
            return TrialErrors.ExperimentConditionsMissing();

        return new TrialRobustness(
            OutOfSampleStart: OutOfSampleStart,
            WalkForward: WalkForward?.ToDomain(),
            Neighborhood: Neighborhood?.ToDomain());
    }
}
