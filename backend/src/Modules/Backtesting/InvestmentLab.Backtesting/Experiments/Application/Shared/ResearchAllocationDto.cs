using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchAllocationDto(
    decimal Cash,
    IReadOnlyList<ResearchTargetDto> Targets,
    IReadOnlyList<ResearchPositionWeightDto> Positions)
{
    public static ResearchAllocationDto From(ReplayAllocation replayAllocation) => new(
        Cash: replayAllocation.Cash,
        Targets: [.. replayAllocation.Targets.Select(ResearchTargetDto.From)],
        Positions: [.. replayAllocation.Positions.Select(ResearchPositionWeightDto.From)]);
}
