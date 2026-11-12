using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchSeriesBriefDto(
    string Code,
    string Label,
    ResearchSummaryDto Summary,
    ResearchHorizonsDto Horizons,
    IReadOnlyList<ResearchPointDto>? Points)
{
    public static ResearchSeriesBriefDto From(ReplaySeriesBrief replaySeriesBrief) => new(
        Code: replaySeriesBrief.Code,
        Label: replaySeriesBrief.Label,
        Summary: ResearchSummaryDto.From(replaySeriesBrief.Summary),
        Horizons: ResearchHorizonsDto.From(replaySeriesBrief.Horizons),
        Points: replaySeriesBrief.Points?.Select(ResearchPointDto.From).ToList());
}
