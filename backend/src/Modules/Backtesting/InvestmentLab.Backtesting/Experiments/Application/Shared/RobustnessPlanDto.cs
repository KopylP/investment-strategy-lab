using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record RobustnessPlanDto(
    DateOnly? OutOfSampleStart,
    WalkForwardWindowsDto? WalkForward,
    ParameterNeighborhoodDto? Neighborhood)
{
    public static RobustnessPlanDto From(RobustnessPlan robustnessPlan) => new(
        OutOfSampleStart: robustnessPlan.OutOfSampleStart,
        WalkForward: robustnessPlan.WalkForward is { } walkForward ? WalkForwardWindowsDto.From(walkForward) : null,
        Neighborhood: robustnessPlan.Neighborhood is { } neighborhood ? ParameterNeighborhoodDto.From(neighborhood) : null);

    public RobustnessPlan ToDomain() => new(
        OutOfSampleStart: OutOfSampleStart,
        WalkForward: WalkForward?.ToDomain(),
        Neighborhood: Neighborhood?.ToDomain());
}
