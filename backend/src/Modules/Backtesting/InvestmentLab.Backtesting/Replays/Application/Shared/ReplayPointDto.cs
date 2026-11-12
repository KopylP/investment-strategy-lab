using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayPointDto(
    DateOnly Date,
    double Growth,
    double WorstDrawdown)
{
    public static ReplayPointDto From(ReplayPoint replayPoint) => new(
        Date: replayPoint.Date,
        Growth: replayPoint.Growth,
        WorstDrawdown: replayPoint.WorstDrawdown);
}
