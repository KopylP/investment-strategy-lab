using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class DomainRulesOfReportsTests
{
    private static readonly Guid Etf = Guid.NewGuid(), Other = Guid.NewGuid();

    private static AccountReport Report(string code, ReportKind kind, params Guid[] subs) => new(
        code, code, "", "mode", kind, [.. subs.Select(id => new SubPortfolioReport(id, "sub", "strategy", null!))]);

    [Fact]
    public void Without_a_code_the_monthly_report_is_chosen()
    {
        var reports = new[] { Report("risk", ReportKind.RiskReview), Report("rebalance", ReportKind.Monthly) };

        AccountReport.Choose(reports, null)!.Code.ShouldBe("rebalance");
        AccountReport.Choose(reports, "risk")!.Code.ShouldBe("risk");
        AccountReport.Choose(reports, "missing").ShouldBeNull();
    }

    [Fact]
    public void Only_wanted_sub_portfolios_are_requested_and_only_a_monthly_report_takes_a_contribution()
    {
        var report = Report("rebalance", ReportKind.Monthly, Etf, Other);

        report.SubPortfoliosWithin(null).ShouldBe([Etf, Other]);
        report.SubPortfoliosWithin(new HashSet<Guid> { Other }).ShouldBe([Other]);
        report.ContributionOf(100m).ShouldBe(100m);
        Report("risk", ReportKind.RiskReview).ContributionOf(100m).ShouldBeNull();
    }

    [Fact]
    public void The_remaining_contribution_is_the_planned_part_of_the_monthly_sum_minus_what_was_paid()
    {
        var settings = PlanSettings.DefaultFor(Guid.NewGuid(), UserId.Of(Guid.NewGuid()));
        settings.Change(1000m, settings.Policy, true, DateTimeOffset.UnixEpoch);

        settings.RemainingContribution(contributed: 100m, plannedShares: 0.5m, allShares: 1m).ShouldBe(400m);
        settings.RemainingContribution(contributed: 900m, plannedShares: 0.5m, allShares: 1m).ShouldBe(0m);
        settings.RemainingContribution(contributed: 0m, plannedShares: 0m, allShares: 0m).ShouldBe(1000m);
    }

    [Fact]
    public void A_standing_values_holdings_at_the_close_or_else_at_their_average_cost()
    {
        var standing = SubPortfolioStanding.Valued(Etf, 0.5m, 10m, [new HoldingValuation(2m, 100m, 50m), new HoldingValuation(3m, null, 50m)]);

        standing.Value.ShouldBe(360m);
    }

    [Fact]
    public void An_exit_signal_on_a_priced_holding_becomes_an_order_and_anything_else_a_note()
    {
        RiskSignal[] signals = [new(Etf, "exit", "стоп"), new(Other, "exit", "немає ціни"), new(Etf, "warning", "увага")];
        var held = new Dictionary<Guid, decimal> { [Etf] = 2m, [Other] = 1m };
        var closes = new Dictionary<Guid, decimal> { [Etf] = 10m };

        var plan = ExitPlan.Of(signals, held, closes, id => id == Etf ? "ETF" : "OTHER");

        plan.Orders.ShouldHaveSingleItem().ShouldBe(new PlannedOrder(OrderKind.Exit, Etf, 2m, 10m, 20m, Message.Of("report.reason.exit_signal", new { reason = "стоп" })));
        plan.Notes.ShouldBe(["OTHER: немає ціни", "ETF: увага"]);
    }
}
