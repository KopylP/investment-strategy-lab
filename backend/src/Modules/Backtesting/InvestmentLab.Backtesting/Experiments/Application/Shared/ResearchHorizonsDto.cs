using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchHorizonsDto(
    double? OneYear,
    double? ThreeYearsAnnual,
    double? FiveYearsAnnual,
    IReadOnlyList<ResearchCalendarYearDto> Years)
{
    public static ResearchHorizonsDto From(SeriesHorizons seriesHorizons) => new(
        OneYear: seriesHorizons.OneYear,
        ThreeYearsAnnual: seriesHorizons.ThreeYearsAnnual,
        FiveYearsAnnual: seriesHorizons.FiveYearsAnnual,
        Years: [.. seriesHorizons.Years.Select(ResearchCalendarYearDto.From)]);
}
