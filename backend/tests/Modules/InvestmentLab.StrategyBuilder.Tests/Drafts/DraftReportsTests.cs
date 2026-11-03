using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftReportsTests
{
    [Fact]
    public void A_new_draft_copies_the_declared_report_settings()
    {
        var monthly = new ReportTiming("monthly", 1, "sunday", "20:00", 0);
        var declared = new DeclaredDraftReport("rebalance", "name", "description", monthly, ["volatility-index"]);

        var report = DraftReports.DefaultsOf([declared]).ShouldHaveSingleItem();

        report.Code.ShouldBe("rebalance");
        report.Schedule.ShouldBe(monthly);
        report.SignalKinds.ShouldBe(["volatility-index"]);
        report.SignalIds.ShouldBeEmpty();
    }
}
