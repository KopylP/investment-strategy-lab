using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayTargetDto(
    Guid InstrumentId,
    decimal Weight)
{
    public static ReplayTargetDto From(ReplayTarget replayTarget) => new(
        InstrumentId: replayTarget.InstrumentId,
        Weight: replayTarget.Weight);
}
