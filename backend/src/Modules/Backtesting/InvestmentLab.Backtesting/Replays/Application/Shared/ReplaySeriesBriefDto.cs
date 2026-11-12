using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplaySeriesBriefDto(
    string Code,
    string Label,
    ReplaySummaryDto Summary,
    SeriesHorizonsDto Horizons,
    IReadOnlyList<ReplayPointDto>? Points)
{
    public static ReplaySeriesBriefDto From(ReplaySeriesBrief replaySeriesBrief) => new(
        Code: replaySeriesBrief.Code,
        Label: replaySeriesBrief.Label,
        Summary: ReplaySummaryDto.From(replaySeriesBrief.Summary),
        Horizons: SeriesHorizonsDto.From(replaySeriesBrief.Horizons),
        Points: replaySeriesBrief.Points?.Select(ReplayPointDto.From).ToList());
}
