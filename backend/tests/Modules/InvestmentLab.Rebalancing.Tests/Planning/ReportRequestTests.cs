using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class ReportRequestTests
{
    private static readonly Guid Eu = Guid.NewGuid(), Us = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 19, 0, 0, TimeSpan.Zero);

    private static ReportRequest Request() => ReportRequest.Create(
        Guid.NewGuid(), UserId.Of(Guid.NewGuid()), "rebalance", "Ребаланс", ReportKind.Monthly, ReportTrigger.Scheduled, null, new DateOnly(2026, 11, 1), [Eu, Us], Now);

    private static StrategyJob Job(string id) => new(id, "Decide", Guid.NewGuid(), 1, null);

    [Fact]
    public void A_request_is_ready_once_every_strategy_it_waits_for_has_answered_or_failed()
    {
        var request = Request();
        request.IsReady.ShouldBeFalse();

        request.PlanRuns([Eu, Us]);
        request.Submitted(Eu, Job("a"));
        request.Submitted(Us, Job("b"));
        request.Answered(Eu, "{}");
        request.IsReady.ShouldBeFalse();

        request.RunFailed(Us, "strategy.failed", "boom");
        request.IsReady.ShouldBeTrue();
        request.Status.ShouldBe(ReportRequestStatus.Pending);
    }

    [Fact]
    public void A_risk_review_with_nothing_held_waits_for_no_strategy()
    {
        var request = Request();

        request.PlanRuns([]);

        request.IsReady.ShouldBeTrue();
    }

    [Fact]
    public void A_lost_job_is_submitted_again_and_counted()
    {
        var request = Request();
        request.PlanRuns([Eu]);
        request.Submitted(Eu, Job("a"));

        request.JobLost(Eu);
        request.RunOf(Eu)!.Job.ShouldBeNull();
        request.Submitted(Eu, Job("b"));

        (request.RunOf(Eu)!.Job!.JobId, request.RunOf(Eu)!.Submissions).ShouldBe(("b", 2));
    }

    [Fact]
    public void An_answer_is_final()
    {
        var request = Request();
        request.PlanRuns([Eu]);
        request.Answered(Eu, "{\"a\":1}");

        request.RunFailed(Eu, "strategy.failed", "late");
        request.JobLost(Eu);

        request.RunOf(Eu).ShouldBe(new StrategyRun(Eu, Answer: "{\"a\":1}"));
    }

    [Fact]
    public void Strategies_still_thinking_at_the_deadline_count_as_not_answered()
    {
        var request = Request();
        request.PlanRuns([Eu, Us]);
        request.Answered(Eu, "{}");
        request.Submitted(Us, Job("b"));

        request.TimeOutWhenDue(Now + ReportRequest.Patience - TimeSpan.FromMinutes(1)).ShouldBeFalse();
        request.TimeOutWhenDue(Now + ReportRequest.Patience).ShouldBeTrue();

        request.RunOf(Us)!.ErrorCode.ShouldBe(ReportRequest.TimedOut);
        request.RunOf(Eu)!.Answer.ShouldBe("{}");
        request.IsReady.ShouldBeTrue();
    }

    [Fact]
    public void A_request_is_completed_or_failed_once()
    {
        var completed = Request();
        var report = ReportId.New();
        completed.Complete(report, Now);
        completed.Fail("too late", Now);
        (completed.Status, completed.ReportId, completed.Error, completed.IsReady).ShouldBe((ReportRequestStatus.Completed, report, null, false));

        var failed = Request();
        failed.Fail(new string('x', 3000), Now);
        failed.Complete(ReportId.New(), Now);
        (failed.Status, failed.ReportId, failed.Error!.Length).ShouldBe((ReportRequestStatus.Failed, (ReportId?)null, ReportRequest.MaxErrorLength));
    }
}
