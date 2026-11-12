using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayPositionWeightDto(
    Guid InstrumentId,
    decimal Quantity,
    decimal Weight)
{
    public static ReplayPositionWeightDto From(ReplayPositionWeight replayPositionWeight) => new(
        InstrumentId: replayPositionWeight.InstrumentId,
        Quantity: replayPositionWeight.Quantity,
        Weight: replayPositionWeight.Weight);
}
