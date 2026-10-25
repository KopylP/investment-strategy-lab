using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Reports;

public sealed class RebalancingReportTests
{
    private static readonly Guid Eu = Guid.NewGuid(), Us = Guid.NewGuid(), Etf = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Monday = new(2026, 11, 2);

    private static RebalancingReport Report(params ActionDraft[] actions) => RebalancingReport.Generate(new ReportDraft(
        Guid.NewGuid(), UserId.Of(Guid.NewGuid()), "XTB", Currency.EUR, YearMonth.Of(2026, 11), ReportKind.Monthly, ReportTrigger.OnDemand,
        Monday, ContributionPlan.None, [], actions, [], null), Now);

    private static ActionDraft Buy(decimal quantity = 10m) =>
        new(ActionKind.Buy, Etf, quantity, 50m, [new ActionAllocation(Eu, quantity * 0.6m, null), new ActionAllocation(Us, quantity * 0.4m, null)], "buy");

    [Fact]
    public void Partial_fills_add_up_and_the_last_one_completes_the_action_and_the_report()
    {
        var report = Report(Buy());
        var action = report.Actions[0];

        report.RecordPartialFill(action.Id, 4m, 49m, 1m, Monday, null, Now).IsSuccess.ShouldBeTrue();
        (action.Status, report.Status).ShouldBe((ActionStatus.PartiallyExecuted, ReportStatus.InProgress));

        var last = report.RecordPartialFill(action.Id, 6m, 50m, 1m, Monday, null, Now);
        last.Value.Reference.FillNumber.ShouldBe(2);
        (action.Status, report.Status).ShouldBe((ActionStatus.Executed, ReportStatus.Closed));
        report.MarkExecuted(action.Id, 1m, 50m, 0m, Monday, null, Now).Error.Code.ShouldBe("report.not_open");
    }

    [Fact]
    public void Undo_reopens_a_closed_report_and_fill_numbers_never_repeat()
    {
        var report = Report(Buy());
        var action = report.Actions[0];
        report.MarkExecuted(action.Id, 10m, 50m, 1m, Monday, null, Now);

        var undone = report.UndoMark(action.Id);

        undone.Value.ShouldHaveSingleItem().Reference.ToString().ShouldBe($"rebalancing:{report.Id}:{action.Id}:1");
        (action.Status, report.Status).ShouldBe((ActionStatus.Pending, ReportStatus.New));
        report.MarkExecuted(action.Id, 10m, 50m, 1m, Monday, null, Now).Value.Fill.Number.ShouldBe(2);
    }

    [Fact]
    public void A_rejected_fill_is_taken_back_with_the_reason()
    {
        var report = Report(Buy(), Buy());
        var action = report.Actions[0];
        var marked = report.MarkExecuted(action.Id, 10m, 50m, 1m, Monday, null, Now).Value;

        report.ApplyTransactionRejected(marked.Reference, "not enough units");

        (action.Status, action.Rejection, report.Status).ShouldBe((ActionStatus.Pending, "not enough units", ReportStatus.New));
    }

    [Fact]
    public void Only_pending_actions_can_be_skipped_and_closing_skips_the_unmarked()
    {
        var report = Report(Buy(), Buy(), Buy());
        var (first, second, third) = (report.Actions[0], report.Actions[1], report.Actions[2]);
        report.RecordPartialFill(first.Id, 2m, 50m, 0m, Monday, null, Now);

        report.Skip(first.Id, "changed mind", Now).Error.Code.ShouldBe("report.action_not_pending");
        report.Skip(second.Id, "too small", Now).IsSuccess.ShouldBeTrue();
        report.Close(Now).IsSuccess.ShouldBeTrue();

        (first.Status, second.SkipReason, third.SkipReason).ShouldBe((ActionStatus.PartiallyExecuted, "too small", ReportAction.ClosedWithoutMark));
        report.Status.ShouldBe(ReportStatus.Closed);
    }

    [Fact]
    public void A_fill_is_split_between_sleeves_in_plan_proportion_and_adds_up_exactly()
    {
        var action = Report(Buy(3m)).Actions[0];

        var parts = action.AllocateFill(1m);

        parts.Sum(part => part.Quantity).ShouldBe(1m);
        parts.Select(part => part.SubPortfolioId).ShouldBe([Eu, Us]);
        parts[0].Quantity.ShouldBe(0.6m);
    }

    [Fact]
    public void A_report_without_actions_is_closed_at_once_and_a_superseded_one_cannot_be_marked()
    {
        Report().Status.ShouldBe(ReportStatus.Closed);

        var report = Report(Buy());
        report.SupersedeBy(ReportId.New(), Now).IsSuccess.ShouldBeTrue();
        report.MarkExecuted(report.Actions[0].Id, 1m, 50m, 0m, Monday, null, Now).Error.Code.ShouldBe("report.not_open");
        report.UndoMark(report.Actions[0].Id).Error.Code.ShouldBe("report.not_open");
    }
}
