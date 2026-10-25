namespace InvestmentLab.SharedKernel.Tests;

public sealed class YearMonthTests
{
    [Fact]
    public void Of_validates_month()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => YearMonth.Of(2026, 13));
        Should.Throw<ArgumentOutOfRangeException>(() => YearMonth.Of(2026, 0));
    }

    [Fact]
    public void Contains_date_boundaries()
    {
        var period = YearMonth.Of(2024, 2);

        period.FirstDay.ShouldBe(new DateOnly(2024, 2, 1));
        period.LastDay.ShouldBe(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public void Moves_across_year_end()
    {
        YearMonth.Of(2026, 12).Next().ShouldBe(YearMonth.Of(2027, 1));
        YearMonth.Of(2026, 1).Previous().ShouldBe(YearMonth.Of(2025, 12));
    }

    [Fact]
    public void Is_built_from_date()
    {
        YearMonth.Containing(new DateOnly(2026, 10, 25)).ShouldBe(YearMonth.Of(2026, 10));
    }

    [Fact]
    public void Round_trips_through_text()
    {
        YearMonth.Of(2026, 3).ToString().ShouldBe("2026-03");
        YearMonth.TryParse("2026-03").Value.ShouldBe(YearMonth.Of(2026, 3));
        YearMonth.TryParse("2026/03").IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Is_ordered()
    {
        (YearMonth.Of(2026, 1) < YearMonth.Of(2026, 2)).ShouldBeTrue();
        (YearMonth.Of(2025, 12) < YearMonth.Of(2026, 1)).ShouldBeTrue();
    }
}
