using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class SeriesHorizonsTests
{
    private const double MonthlyGain = 0.01;

    private static readonly double YearOfMonthlyGains = Math.Pow(1d + MonthlyGain, 12) - 1d;

    [Fact]
    public void Trailing_returns_are_anchored_at_the_last_point_and_annualized_beyond_one_year()
    {
        var start = new DateOnly(2020, 1, 1);
        var points = MonthEndsGrowing(start, months: 48);

        var horizons = SeriesHorizons.Of(start, points);

        horizons.OneYear!.Value.ShouldBe(YearOfMonthlyGains, 0.001);
        horizons.ThreeYearsAnnual!.Value.ShouldBe(YearOfMonthlyGains, 0.001);
    }

    [Fact]
    public void A_horizon_longer_than_the_series_is_absent()
    {
        var start = new DateOnly(2020, 1, 1);
        var points = MonthEndsGrowing(start, months: 48);

        var horizons = SeriesHorizons.Of(start, points);

        horizons.FiveYearsAnnual.ShouldBeNull();
    }

    [Fact]
    public void Calendar_years_not_covering_january_to_december_are_partial()
    {
        var start = new DateOnly(2020, 3, 2);
        var points = MonthEndsGrowing(start, months: 24);

        var horizons = SeriesHorizons.Of(start, points);

        horizons.Years.Select(year => year.Year).ShouldBe([2020, 2021, 2022]);
        horizons.Years.Select(year => year.Partial).ShouldBe([true, false, true]);
        horizons.Years[1].Return.ShouldBe(YearOfMonthlyGains, 0.0001);
        horizons.Years[0].Return.ShouldBe(Math.Pow(1d + MonthlyGain, 10) - 1d, 0.0001);
    }

    [Fact]
    public void A_series_ending_mid_month_uses_its_last_point_as_the_end()
    {
        var start = new DateOnly(2020, 1, 1);
        var points = MonthEndsGrowing(start, months: 24).ToList();
        var midMonth = new ReplayPoint(new DateOnly(2022, 1, 14), points[^1].Growth * 1.05, 0d);
        points.Add(midMonth);

        var horizons = SeriesHorizons.Of(start, points);

        var yearBefore = points.Last(point => point.Date <= new DateOnly(2021, 1, 14));
        horizons.OneYear!.Value.ShouldBe(midMonth.Growth / yearBefore.Growth - 1d, 0.0001);
        horizons.Years[^1].ShouldBe(new CalendarYearReturn(2022, 0.05, Partial: true), new CalendarYearComparer());
    }

    private static List<ReplayPoint> MonthEndsGrowing(DateOnly start, int months)
    {
        var points = new List<ReplayPoint>(months);
        var growth = 1d;
        var month = new DateOnly(start.Year, start.Month, 1);

        for (var index = 0; index < months; index++)
        {
            growth *= 1d + MonthlyGain;
            var monthEnd = month.AddMonths(index + 1).AddDays(-1);
            points.Add(new ReplayPoint(monthEnd, growth, 0d));
        }

        return points;
    }

    private sealed class CalendarYearComparer : IEqualityComparer<CalendarYearReturn>
    {
        public bool Equals(CalendarYearReturn? x, CalendarYearReturn? y) =>
            x is not null && y is not null && x.Year == y.Year && x.Partial == y.Partial && Math.Abs(x.Return - y.Return) < 0.0001;

        public int GetHashCode(CalendarYearReturn obj) => obj.Year;
    }
}
