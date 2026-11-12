using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Tests;

public sealed class DashboardSummaryTests
{
    [Fact]
    public void A_history_whose_value_went_below_zero_shows_no_annualized_return()
    {
        var start = new DateOnly(2024, 1, 2);
        var end = start.AddDays(800);
        Snapshot[] history =
        [
            new(start, null, Value: 100m, Cash: 100m, Flow: 100m, NetContributions: 100m, TwrIndex: 1m),
            new(end, null, Value: -20m, Cash: -220m, Flow: 0m, NetContributions: 100m, TwrIndex: -0.2m),
        ];

        var summary = DashboardSummary.Of(history, null, "ALL");

        summary.Dashboard.Twr.ShouldBe(-1.2m);
        summary.Dashboard.TwrAnnualized.ShouldBeNull();
    }
}
