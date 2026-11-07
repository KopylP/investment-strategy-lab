namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record SeriesHorizons(double? OneYear, double? ThreeYearsAnnual, double? FiveYearsAnnual, IReadOnlyList<CalendarYearReturn> Years)
{
    private const double StartingGrowth = 1d;
    private const double DaysInYear = 365.25;
    private const int FirstMonth = 1;
    private const int LastMonth = 12;

    public static SeriesHorizons Of(DateOnly start, IReadOnlyList<ReplayPoint> points)
    {
        var anchors = AnchorsOf(start, points);

        return new SeriesHorizons(
            OneYear: TrailingReturn(anchors, years: 1),
            ThreeYearsAnnual: TrailingAnnualReturn(anchors, years: 3),
            FiveYearsAnnual: TrailingAnnualReturn(anchors, years: 5),
            Years: CalendarYearsOf(anchors));
    }

    private static List<ReplayPoint> AnchorsOf(DateOnly start, IReadOnlyList<ReplayPoint> points)
    {
        var startsBeforePoints = points.Count == 0 || start < points[0].Date;
        if (!startsBeforePoints)
            return [.. points];

        var startingPoint = new ReplayPoint(start, StartingGrowth, 0d);

        return [startingPoint, .. points];
    }

    private static double? TrailingReturn(List<ReplayPoint> anchors, int years)
    {
        if (WindowStart(anchors, years) is not { } windowStart)
            return null;

        return anchors[^1].Growth / windowStart.Growth - 1d;
    }

    private static double? TrailingAnnualReturn(List<ReplayPoint> anchors, int years)
    {
        if (WindowStart(anchors, years) is not { } windowStart)
            return null;

        var end = anchors[^1];
        var spanInYears = (end.Date.DayNumber - windowStart.Date.DayNumber) / DaysInYear;

        return Math.Pow(end.Growth / windowStart.Growth, 1d / spanInYears) - 1d;
    }

    private static ReplayPoint? WindowStart(List<ReplayPoint> anchors, int years)
    {
        var target = anchors[^1].Date.AddYears(-years);

        return anchors.LastOrDefault(anchor => anchor.Date <= target);
    }

    private static List<CalendarYearReturn> CalendarYearsOf(List<ReplayPoint> anchors)
    {
        var years = anchors.Select(anchor => anchor.Date.Year).Distinct();

        return [.. years.Select(year => CalendarYearOf(anchors, year)).OfType<CalendarYearReturn>()];
    }

    private static CalendarYearReturn? CalendarYearOf(List<ReplayPoint> anchors, int year)
    {
        var yearEnd = anchors.Last(anchor => anchor.Date.Year == year);
        var priorYearEnd = anchors.LastOrDefault(anchor => anchor.Date.Year < year);
        var yearBegin = priorYearEnd ?? anchors.First(anchor => anchor.Date.Year == year);
        if (yearBegin == yearEnd)
            return null;

        var coversJanuary = priorYearEnd is not null || yearBegin.Date.Month == FirstMonth;
        var coversDecember = yearEnd.Date.Month == LastMonth;
        var yearReturn = yearEnd.Growth / yearBegin.Growth - 1d;

        return new CalendarYearReturn(year, yearReturn, Partial: !(coversJanuary && coversDecember));
    }
}
