using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class TrialCleanupTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_cleanup_is_due_immediately()
    {
        var strategyId = Guid.CreateVersion7();
        var cleanup = TrialCleanup.For(strategyId, Now);

        cleanup.TrialStrategyId.ShouldBe(strategyId);
        cleanup.Attempts.ShouldBe(0);
        cleanup.NextAttemptAt.ShouldBe(Now);
    }

    [Fact]
    public void A_failed_cleanup_is_kept_for_the_job()
    {
        var cleanup = TrialCleanup.For(Guid.CreateVersion7(), Now);

        cleanup.Failed(Now);
        cleanup.Failed(Now);

        cleanup.Attempts.ShouldBe(2);
        cleanup.NextAttemptAt.ShouldBe(Now.AddMinutes(20));
    }

    [Fact]
    public void Cleanup_retries_wait_at_most_a_day()
    {
        var cleanup = TrialCleanup.For(Guid.CreateVersion7(), Now);
        for (var attempt = 0; attempt < 100; attempt++)
            cleanup.Failed(Now);

        cleanup.NextAttemptAt.ShouldBe(Now.AddDays(1));
    }
}
