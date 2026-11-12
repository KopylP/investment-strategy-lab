using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Backtesting.Replays.Application.RunBacktest;

public sealed class BacktestsJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<BacktestsJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = new IntervalSchedule(TimeSpan.FromSeconds(15));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (await NextAsync(cancellationToken) is { } backtestId)
            await RunOneAsync(backtestId, cancellationToken);
    }

    private async Task<Guid?> NextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IBacktestRepository>().NextToRunAsync(cancellationToken);
    }

    private async Task RunOneAsync(Guid backtestId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();

            await scope.ServiceProvider.GetRequiredService<RunBacktestHandler>().Handle(backtestId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Backtest {BacktestId} failed", backtestId);
            await FailAsync(backtestId, cancellationToken);
        }
    }

    private async Task FailAsync(Guid backtestId, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var backtests = scope.ServiceProvider.GetRequiredService<IBacktestRepository>();

        if (await backtests.FindAsync(backtestId, cancellationToken) is not { } backtest)
            return;

        backtest.Fail(Message.Of("backtest.failed"), clock.GetUtcNow());
        await backtests.SaveChangesAsync(cancellationToken);
    }
}
