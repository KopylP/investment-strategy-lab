using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchSeriesDto(
    string Code,
    string Label,
    ResearchSummaryDto Summary,
    IReadOnlyList<ResearchPointDto> Points)
{
    public static ResearchSeriesDto From(ReplaySeries replaySeries) => new(
        Code: replaySeries.Code,
        Label: replaySeries.Label,
        Summary: ResearchSummaryDto.From(replaySeries.Summary),
        Points: [.. replaySeries.Points.Select(ResearchPointDto.From)]);
}
