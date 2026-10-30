using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class ReportCadenceTests
{
    [Theory]
    [InlineData("weekly", "2026-11-08", true)]      // second Sunday
    [InlineData("monthly", "2026-11-08", false)]
    [InlineData("monthly", "2026-11-01", true)]     // first Sunday
    [InlineData("quarterly", "2026-11-01", false)]
    [InlineData("quarterly", "2027-01-03", true)]   // first Sunday of the quarter
    [InlineData("monthly", "2026-11-02", false)]    // a Monday
    [InlineData("off", "2026-11-01", false)]
    public void Report_days_follow_the_frequency_the_strategy_declares(string frequency, string day, bool expected) =>
        ReportCadence.Of(frequency, "20:00").IsDue(DateOnly.Parse(day)).ShouldBe(expected);

    [Fact]
    public void Next_dates_are_found_and_a_report_switched_off_has_none()
    {
        ReportCadence.Of("quarterly", "20:00").Next(new DateOnly(2026, 10, 25)).ShouldBe(new DateOnly(2027, 1, 3));
        ReportCadence.Of("monthly", "20:00").Next(new DateOnly(2026, 10, 25)).ShouldBe(new DateOnly(2026, 11, 1));
        ReportCadence.Of("off", "19:00").Next(new DateOnly(2026, 10, 25)).ShouldBeNull();
    }

    [Fact]
    public void An_unknown_frequency_is_off_and_the_time_is_the_declared_one()
    {
        ReportCadence.Of("daily", "19:30").ShouldBe(new ReportCadence(ReportFrequency.Off, new TimeOnly(19, 30)));
    }
}
