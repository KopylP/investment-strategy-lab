using InvestmentLab.Rebalancing.Reports.Application.CloseReport;
using InvestmentLab.Rebalancing.Reports.Application.MarkActionExecuted;
using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Application.SkipAction;
using InvestmentLab.Rebalancing.Reports.Application.UndoMark;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Reports;

public sealed class ReportChangedMeanwhileTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly RebalancingReport _report = RebalancingReport.Generate(new ReportDraft(
        Guid.NewGuid(), UserId.Of(Guid.NewGuid()), "XTB", Currency.EUR, YearMonth.Of(2026, 11), ReportKind.Monthly, ReportTrigger.OnDemand,
        Today, ContributionPlan.None, [], [Buy(), Buy()], [], null, "rebalance", "Rebalance"), Now);

    private static ActionDraft Buy() =>
        new(ActionKind.Buy, Guid.NewGuid(), 10m, 50m, [new ActionAllocation(Guid.NewGuid(), 10m, null)], "buy");

    private sealed class ReportsChangedByAnotherSave(RebalancingReport report) : IReportRepository
    {
        public Task<RebalancingReport?> FindAsync(ReportId reportId, CancellationToken cancellationToken) =>
            Task.FromResult<RebalancingReport?>(reportId == report.Id ? report : null);

        public Task<IReadOnlyList<RebalancingReport>> ListAsync(Guid? accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RebalancingReport>>([report]);

        public Task<IReadOnlyList<RebalancingReport>> ForPeriodAsync(Guid accountId, YearMonth period, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RebalancingReport>>([report]);

        public Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);

        public Task<IReadOnlyList<RebalancingReport>> OpenAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RebalancingReport>>([report]);

        public void Add(RebalancingReport added) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("A request of the user saves only through TrySaveChangesAsync.");

        public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private Guid ReportId => _report.Id.Value;

    private Guid FirstActionId => _report.Actions[0].Id.Value;

    private ReportsChangedByAnotherSave Reports() => new(_report);

    private static TimeProvider Clock() => new FixedClock(Now);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static void ShouldBeChangedMeanwhile(Result result)
    {
        result.IsFailure.ShouldBeTrue();
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Code.ShouldBe("report.changed_meanwhile");
    }

    [Fact]
    public async Task Marking_an_action_while_the_report_was_saved_by_someone_else_is_a_conflict_not_a_lost_update()
    {
        var handler = new MarkActionExecutedHandler(new FillMarker(Reports(), Clock()));

        var marked = await handler.Handle(ReportId, FirstActionId, new MarkActionExecutedCommand(Price: 50m), TestContext.Current.CancellationToken);

        ShouldBeChangedMeanwhile(marked);
    }

    [Fact]
    public async Task Skipping_an_action_while_the_report_was_saved_by_someone_else_is_a_conflict()
    {
        var handler = new SkipActionHandler(Reports(), Clock());

        var skipped = await handler.Handle(ReportId, FirstActionId, new SkipActionCommand("too small"), TestContext.Current.CancellationToken);

        ShouldBeChangedMeanwhile(skipped);
    }

    [Fact]
    public async Task Undoing_a_mark_while_the_report_was_saved_by_someone_else_is_a_conflict()
    {
        _report.MarkExecuted(_report.Actions[0].Id, 10m, 50m, 0m, Today, null, Now);
        var handler = new UndoMarkHandler(Reports(), Clock());

        var undone = await handler.Handle(ReportId, FirstActionId, TestContext.Current.CancellationToken);

        ShouldBeChangedMeanwhile(undone);
    }

    [Fact]
    public async Task Closing_a_report_while_it_was_saved_by_someone_else_is_a_conflict()
    {
        var handler = new CloseReportHandler(Reports(), Clock());

        var closed = await handler.Handle(ReportId, TestContext.Current.CancellationToken);

        ShouldBeChangedMeanwhile(closed);
    }
}
