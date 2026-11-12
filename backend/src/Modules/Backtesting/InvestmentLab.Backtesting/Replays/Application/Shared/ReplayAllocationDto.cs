using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayAllocationDto(
    decimal Cash,
    IReadOnlyList<ReplayTargetDto> Targets,
    IReadOnlyList<ReplayPositionWeightDto> Positions)
{
    public static ReplayAllocationDto From(ReplayAllocation replayAllocation) => new(
        Cash: replayAllocation.Cash,
        Targets: [.. replayAllocation.Targets.Select(ReplayTargetDto.From)],
        Positions: [.. replayAllocation.Positions.Select(ReplayPositionWeightDto.From)]);
}
