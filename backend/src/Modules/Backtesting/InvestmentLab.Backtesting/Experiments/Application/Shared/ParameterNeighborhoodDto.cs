using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ParameterNeighborhoodDto(
    string Path,
    IReadOnlyList<decimal> Values)
{
    public static ParameterNeighborhoodDto From(ParameterNeighborhood parameterNeighborhood) => new(
        Path: parameterNeighborhood.Path,
        Values: parameterNeighborhood.Values);

    public ParameterNeighborhood ToDomain() => new(
        Path: Path,
        Values: Values);
}
