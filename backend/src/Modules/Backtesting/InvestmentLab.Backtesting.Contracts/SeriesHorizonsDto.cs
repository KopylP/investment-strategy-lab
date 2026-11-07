namespace InvestmentLab.Backtesting.Contracts;

public sealed record SeriesHorizonsDto(double? OneYear, double? ThreeYearsAnnual, double? FiveYearsAnnual, IReadOnlyList<CalendarYearReturnDto> Years);
