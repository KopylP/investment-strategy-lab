using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.CleanUpTrials;

public sealed class TrialCleanupJob(ITrialCleanupRepository cleanups, IServiceScopeFactory scopes, TimeProvider clock, ILogger<TrialCleanupJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = new IntervalSchedule(TimeSpan.FromMinutes(5));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var due = await cleanups.DueAsync(clock.GetUtcNow(), cancellationToken);
        foreach (var cleanupId in due)
            await CleanInOwnScopeAsync(cleanupId, cancellationToken);
    }

    private async Task CleanInOwnScopeAsync(Guid cleanupId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<CleanUpTrialHandler>();

            await handler.Handle(cleanupId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Trial cleanup {CleanupId} could not be completed", cleanupId);
        }
    }
}
