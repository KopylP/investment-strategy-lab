using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class DraftReportRescheduleTests
{
    private static readonly ReportTiming MonthlyOnTheFirst = new(Frequency: "monthly", Day: 1, Weekday: "sunday", Time: "20:00", Minute: 0);
    private static readonly ReportTiming WeeklyOnSunday = new(Frequency: "weekly", Day: 1, Weekday: "sunday", Time: "20:00", Minute: 0);

    private static readonly Guid Signal = Guid.NewGuid();

    private static readonly IReadOnlyList<DraftReport> Reports =
    [
        new DraftReport(Code: "rebalance", Schedule: MonthlyOnTheFirst, SignalKinds: ["market"], SignalIds: [Signal]),
        new DraftReport(Code: "risk-review", Schedule: WeeklyOnSunday, SignalKinds: [], SignalIds: []),
    ];

    [Fact]
    public void Making_the_rebalance_quarterly_keeps_its_day_time_and_signals()
    {
        var patch = new ReportTimingPatch(Frequency: "quarterly", Day: null, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "rebalance", patch).Value;

        var rebalance = rescheduled.Single(report => report.Code == "rebalance");
        rebalance.Schedule.ShouldBe(MonthlyOnTheFirst with { Frequency = "quarterly" });
        rebalance.SignalKinds.ShouldBe(["market"]);
        rebalance.SignalIds.ShouldBe([Signal]);
    }

    [Fact]
    public void Rescheduling_one_report_leaves_the_others_as_they_are()
    {
        var patch = new ReportTimingPatch(Frequency: "quarterly", Day: 15, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "rebalance", patch).Value;

        rescheduled.Count.ShouldBe(2);
        rescheduled.Single(report => report.Code == "risk-review").Schedule.ShouldBe(WeeklyOnSunday);
    }

    [Fact]
    public void A_frequency_is_stored_in_lower_case()
    {
        var patch = new ReportTimingPatch(Frequency: "Quarterly", Day: null, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "rebalance", patch).Value;

        rescheduled[0].Schedule.Frequency.ShouldBe("quarterly");
    }

    [Fact]
    public void A_report_the_draft_does_not_have_is_refused()
    {
        var patch = new ReportTimingPatch(Frequency: "quarterly", Day: null, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "tax-review", patch);

        rescheduled.Error.Code.ShouldBe("builder.report.unknown");
    }

    [Fact]
    public void An_unknown_frequency_is_refused()
    {
        var patch = new ReportTimingPatch(Frequency: "yearly", Day: null, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "rebalance", patch);

        rescheduled.Error.Code.ShouldBe("cadence.frequency");
    }

    [Fact]
    public void A_day_outside_the_month_is_refused()
    {
        var patch = new ReportTimingPatch(Frequency: "quarterly", Day: 32, Weekday: null, Time: null, Minute: null);

        var rescheduled = DraftReports.Rescheduled(Reports, "rebalance", patch);

        rescheduled.Error.Code.ShouldBe("cadence.day");
    }
}
