using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchPointDto(
    DateOnly Date,
    double Growth,
    double WorstDrawdown)
{
    public static ResearchPointDto From(ReplayPoint replayPoint) => new(
        Date: replayPoint.Date,
        Growth: replayPoint.Growth,
        WorstDrawdown: replayPoint.WorstDrawdown);
}
