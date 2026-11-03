using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplaySeriesTests
{
    [Fact]
    public void A_fall_inside_a_month_stays_visible_after_keeping_only_the_month_end()
    {
        GrowthDay[] days =
        [
            new(new DateOnly(2024, 1, 2), 1.00),
            new(new DateOnly(2024, 1, 15), 0.70),
            new(new DateOnly(2024, 1, 31), 1.00),
            new(new DateOnly(2024, 2, 29), 1.10),
        ];

        var series = ReplaySeries.Of(ReplaySeries.StrategyCode, "Strategy", days, 0d);

        series.Points.Count.ShouldBe(2);
        series.Points[0].Growth.ShouldBe(1.00);
        series.Points[0].WorstDrawdown.ShouldBe(-0.30, 0.0001);
        series.Summary.MaxDrawdown.ShouldBe(-0.30, 0.0001);
        series.Summary.TotalReturn.ShouldBe(0.10, 0.0001);
    }

    [Fact]
    public void Periods_open_by_the_cadence_of_the_report()
    {
        var friday = new DateOnly(2024, 3, 29);
        var monday = new DateOnly(2024, 4, 1);

        ReplayCalendar.OpensPeriod(ReplayCadence.Monthly, friday, monday).ShouldBeTrue();
        ReplayCalendar.OpensPeriod(ReplayCadence.Weekly, friday, monday).ShouldBeTrue();
        ReplayCalendar.OpensPeriod(ReplayCadence.Quarterly, friday, monday).ShouldBeTrue();
        ReplayCalendar.OpensPeriod(ReplayCadence.Monthly, monday, monday.AddDays(1)).ShouldBeFalse();
        ReplayCalendar.OpensPeriod(ReplayCadence.Monthly, null, monday).ShouldBeTrue();
    }
}
