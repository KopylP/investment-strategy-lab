using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchTargetDto(
    Guid InstrumentId,
    decimal Weight)
{
    public static ResearchTargetDto From(ReplayTarget replayTarget) => new(
        InstrumentId: replayTarget.InstrumentId,
        Weight: replayTarget.Weight);
}
