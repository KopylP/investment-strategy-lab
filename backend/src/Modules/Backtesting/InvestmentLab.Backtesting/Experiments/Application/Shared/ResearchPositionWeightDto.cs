using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchPositionWeightDto(
    Guid InstrumentId,
    decimal Quantity,
    decimal Weight)
{
    public static ResearchPositionWeightDto From(ReplayPositionWeight replayPositionWeight) => new(
        InstrumentId: replayPositionWeight.InstrumentId,
        Quantity: replayPositionWeight.Quantity,
        Weight: replayPositionWeight.Weight);
}
