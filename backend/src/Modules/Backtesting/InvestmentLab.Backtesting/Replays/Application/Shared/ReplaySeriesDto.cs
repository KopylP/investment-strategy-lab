using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplaySeriesDto(
    string Code,
    string Label,
    ReplaySummaryDto Summary,
    IReadOnlyList<ReplayPointDto> Points)
{
    public static ReplaySeriesDto From(ReplaySeries replaySeries) => new(
        Code: replaySeries.Code,
        Label: replaySeries.Label,
        Summary: ReplaySummaryDto.From(replaySeries.Summary),
        Points: [.. replaySeries.Points.Select(ReplayPointDto.From)]);
}
