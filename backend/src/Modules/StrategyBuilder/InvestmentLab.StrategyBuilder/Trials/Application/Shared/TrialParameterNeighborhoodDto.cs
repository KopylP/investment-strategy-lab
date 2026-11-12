using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialParameterNeighborhoodDto(string Path, IReadOnlyList<decimal> Values)
{
    public static TrialParameterNeighborhoodDto From(TrialParameterNeighborhood neighborhood) => new(neighborhood.Path, neighborhood.Values);

    public TrialParameterNeighborhood ToDomain() => new(Path, Values);
}
