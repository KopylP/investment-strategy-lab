using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class ReportCadenceTests
{
    [Theory]
    [InlineData("Weekly", "2026-11-08", true)]      // second Sunday
    [InlineData("Monthly", "2026-11-08", false)]
    [InlineData("Monthly", "2026-11-01", true)]     // first Sunday
    [InlineData("Quarterly", "2026-11-01", false)]
    [InlineData("Quarterly", "2027-01-03", true)]   // first Sunday of the quarter
    [InlineData("Monthly", "2026-11-02", false)]    // a Monday
    public void Rebalance_days_follow_the_strategy_frequency(string frequency, string day, bool expected) =>
        ReportCadence.Of(frequency, "Weekly").IsRebalanceDay(DateOnly.Parse(day)).ShouldBe(expected);

    [Fact]
    public void Risk_review_can_be_switched_off_and_next_dates_are_found()
    {
        var quarterly = ReportCadence.Of("quarterly", "off");

        quarterly.IsRiskReviewDay(new DateOnly(2026, 11, 8)).ShouldBeFalse();
        quarterly.NextRiskReview(new DateOnly(2026, 10, 25)).ShouldBeNull();
        quarterly.NextRebalance(new DateOnly(2026, 10, 25)).ShouldBe(new DateOnly(2027, 1, 3));
        ReportCadence.Classic.NextRebalance(new DateOnly(2026, 10, 25)).ShouldBe(new DateOnly(2026, 11, 1));
    }
}
