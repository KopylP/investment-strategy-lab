using InvestmentLab.Backtesting.Replays.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplayRunGateTests
{
    [Fact]
    public async Task A_run_waits_while_every_slot_is_taken_and_starts_when_one_is_freed()
    {
        var options = Options.Create(new ReplayRunOptions { MaxConcurrentRuns = 1 });
        using var gate = new ReplayRunGate(options, NullLogger<ReplayRunGate>.Instance);
        var firstMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationToken = TestContext.Current.CancellationToken;

        var first = gate.RunAsync(() => firstMayFinish.Task, cancellationToken);
        var second = gate.RunAsync(() => SignalStart(secondStarted), cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        var startedWhileTaken = secondStarted.Task.IsCompleted;
        firstMayFinish.SetResult();
        await Task.WhenAll(first, second);

        startedWhileTaken.ShouldBeFalse();
        secondStarted.Task.IsCompleted.ShouldBeTrue();
    }

    private static Task SignalStart(TaskCompletionSource started)
    {
        started.SetResult();

        return Task.CompletedTask;
    }
}
