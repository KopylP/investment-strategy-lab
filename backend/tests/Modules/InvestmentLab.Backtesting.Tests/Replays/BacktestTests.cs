using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class BacktestTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_successful_backtest_is_made_again_after_a_month()
    {
        var backtest = Finished(Now.AddDays(-33), succeeded: true);

        backtest.IsDueAgain(Now).ShouldBeTrue();
        backtest.IsDueAgain(Now.AddDays(-2)).ShouldBeFalse();
    }

    [Fact]
    public void A_failed_backtest_is_tried_again_the_next_day()
    {
        var backtest = Finished(Now.AddHours(-25), succeeded: false);

        backtest.IsDueAgain(Now).ShouldBeTrue();
        backtest.IsDueAgain(Now.AddHours(-2)).ShouldBeFalse();
    }

    [Fact]
    public void A_waiting_backtest_is_not_requested_again()
    {
        var backtest = Backtest.Request(UserId.New(), Guid.NewGuid(), 1, Now.AddDays(-40));

        backtest.IsDueAgain(Now).ShouldBeFalse();
    }

    private static Backtest Finished(DateTimeOffset at, bool succeeded)
    {
        var backtest = Backtest.Request(UserId.New(), Guid.NewGuid(), 1, at);
        backtest.Start();

        if (succeeded)
            backtest.Complete(new ReplayResult(new DateOnly(2008, 1, 2), new DateOnly(2026, 10, 30), 0, 0, 0, []), at);
        else
            backtest.Fail("backtest.failed", at);

        return backtest;
    }
}
