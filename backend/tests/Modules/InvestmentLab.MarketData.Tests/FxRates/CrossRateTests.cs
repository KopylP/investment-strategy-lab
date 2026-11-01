using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.Tests.FxRates;

public sealed class CrossRateTests
{
    private static readonly DateOnly Monday = new(2026, 3, 2), Tuesday = new(2026, 3, 3), Wednesday = new(2026, 3, 4);

    [Fact]
    public void A_cross_rate_divides_both_currencies_through_the_pivot()
    {
        CrossRate.Of(4m, 2m).ShouldBe(2m);
    }

    [Theory]
    [InlineData(null, 2.0)]
    [InlineData(4.0, null)]
    [InlineData(4.0, 0.0)]
    public void A_missing_leg_gives_no_rate(double? fromInPivot, double? toInPivot)
    {
        CrossRate.Of((decimal?)fromInPivot, (decimal?)toInPivot).ShouldBeNull();
    }

    [Fact]
    public void A_day_without_a_new_rate_keeps_the_last_rate_of_that_leg()
    {
        (DateOnly, decimal)[] fromLeg = [(Monday, 4m), (Wednesday, 8m)];
        (DateOnly, decimal)[] toLeg = [(Monday, 2m), (Tuesday, 4m)];

        var series = CrossRate.SeriesOf(fromLeg, toLeg);

        series.ShouldBe([(Monday, 2m), (Tuesday, 1m), (Wednesday, 2m)]);
    }

    [Fact]
    public void The_pivot_itself_counts_as_one_on_every_day()
    {
        (DateOnly, decimal)[] toLeg = [(Monday, 4m)];

        CrossRate.SeriesOf(null, toLeg).ShouldBe([(Monday, 0.25m)]);
    }

    [Fact]
    public void Days_before_a_leg_has_any_rate_are_left_out()
    {
        (DateOnly, decimal)[] fromLeg = [(Monday, 4m), (Tuesday, 6m)];
        (DateOnly, decimal)[] toLeg = [(Tuesday, 2m)];

        CrossRate.SeriesOf(fromLeg, toLeg).ShouldBe([(Tuesday, 3m)]);
    }
}
