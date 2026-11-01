using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Reports;

public sealed class JournalMatcherTests
{
    private static readonly Guid Eu = Guid.NewGuid(), Nasdaq = Guid.NewGuid(), Value = Guid.NewGuid(), Cash = Guid.NewGuid();
    private static readonly DateTimeOffset Generated = new(2026, 11, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Monday = new(2026, 11, 2);

    private static RebalancingReport Report(params ActionDraft[] actions) => RebalancingReport.Generate(new ReportDraft(
        Guid.NewGuid(), UserId.Of(Guid.NewGuid()), "Revolut", Currency.EUR, YearMonth.Of(2026, 11), ReportKind.Monthly, ReportTrigger.Scheduled,
        new DateOnly(2026, 10, 30), ContributionPlan.None, [], actions, [], null), Generated);

    private static ActionDraft Draft(ActionKind kind, Guid? instrument, decimal quantity, decimal price) =>
        new(kind, instrument, quantity, price, [new ActionAllocation(Eu, quantity, null)], kind.ToString());

    private static JournalOperation Operation(JournalOperationKind kind, Guid? instrument, decimal quantity, DateOnly on, decimal price = 16m) =>
        new(Guid.NewGuid(), on, kind, instrument, quantity, price, 0m);

    [Fact]
    public void Trades_after_the_report_mark_the_actions_of_their_instrument_and_direction()
    {
        var report = Report(
            Draft(ActionKind.Contribute, null, 227m, 1m),
            Draft(ActionKind.Sell, Value, 3m, 16m),
            Draft(ActionKind.Buy, Nasdaq, 10m, 16.5m),
            Draft(ActionKind.Park, Cash, 1m, 140m));
        var operations = new[]
        {
            Operation(JournalOperationKind.Buy, Nasdaq, 4m, Monday.AddDays(-5)),
            Operation(JournalOperationKind.Contribution, null, 227m, Monday, 1m),
            Operation(JournalOperationKind.Sell, Value, 3m, Monday),
            Operation(JournalOperationKind.Buy, Nasdaq, 6m, Monday),
            Operation(JournalOperationKind.Buy, Nasdaq, 4m, Monday.AddDays(1)),
            Operation(JournalOperationKind.Buy, Value, 1m, Monday),
        };

        var matches = JournalMatcher.Match(report, operations);

        matches.Select(match => (report.FindAction(match.ActionId)!.Kind, match.Operations.Count, match.Completes))
            .ShouldBe([(ActionKind.Contribute, 1, true), (ActionKind.Sell, 1, true), (ActionKind.Buy, 2, true)]);
    }

    [Fact]
    public void Linking_marks_without_a_new_journal_entry_and_undo_only_unlinks()
    {
        var report = Report(Draft(ActionKind.Buy, Nasdaq, 10m, 16.5m), Draft(ActionKind.Sell, Value, 3m, 16m));
        var bought = Operation(JournalOperationKind.Buy, Nasdaq, 6m, Monday, 16.4m);

        var matches = JournalMatcher.Match(report, [bought]);
        report.LinkJournalOperations(matches, Generated).IsSuccess.ShouldBeTrue();

        var buy = report.Actions[0];
        (buy.Status, report.Status).ShouldBe((ActionStatus.PartiallyExecuted, ReportStatus.InProgress));
        buy.Fills.ShouldHaveSingleItem().ShouldBe(new Fill(1, 6m, 16.4m, 0m, Monday, Message.Of("report.action.from_journal"), bought.TransactionId, Linked: true));

        var rest = Operation(JournalOperationKind.Buy, Nasdaq, 4m, Monday.AddDays(2));
        JournalMatcher.Match(report, [rest]).ShouldHaveSingleItem().Completes.ShouldBeTrue();

        report.UndoMark(buy.Id).Value.ShouldHaveSingleItem().Fill.Linked.ShouldBeTrue();
    }
}
