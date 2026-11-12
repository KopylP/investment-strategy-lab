using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record SeriesHorizonsDto(
    double? OneYear,
    double? ThreeYearsAnnual,
    double? FiveYearsAnnual,
    IReadOnlyList<CalendarYearReturnDto> Years)
{
    public static SeriesHorizonsDto From(SeriesHorizons seriesHorizons) => new(
        OneYear: seriesHorizons.OneYear,
        ThreeYearsAnnual: seriesHorizons.ThreeYearsAnnual,
        FiveYearsAnnual: seriesHorizons.FiveYearsAnnual,
        Years: [.. seriesHorizons.Years.Select(CalendarYearReturnDto.From)]);
}
