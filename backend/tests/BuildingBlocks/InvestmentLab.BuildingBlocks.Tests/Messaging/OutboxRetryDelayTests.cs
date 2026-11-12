using InvestmentLab.BuildingBlocks.Messaging;

namespace InvestmentLab.BuildingBlocks.Tests.Messaging;

public sealed class OutboxRetryDelayTests
{
    [Fact]
    public void The_first_retry_waits_ten_seconds_and_each_next_one_twice_as_long()
    {
        OutboxRetryDelay.After(1).ShouldBe(TimeSpan.FromSeconds(10));
        OutboxRetryDelay.After(2).ShouldBe(TimeSpan.FromSeconds(20));
        OutboxRetryDelay.After(3).ShouldBe(TimeSpan.FromSeconds(40));
    }

    [Fact]
    public void A_retry_never_waits_longer_than_an_hour()
    {
        OutboxRetryDelay.After(10).ShouldBe(TimeSpan.FromHours(1));
        OutboxRetryDelay.After(1000).ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void All_attempts_together_outlast_an_outage_of_fifteen_hours()
    {
        var retries = Enumerable.Range(1, OutboxMessage.MaxAttempts - 1).Select(OutboxRetryDelay.After);

        var lastAttemptAfter = retries.Aggregate(TimeSpan.Zero, (total, delay) => total + delay);

        lastAttemptAfter.ShouldBeGreaterThan(TimeSpan.FromHours(15));
    }
}
