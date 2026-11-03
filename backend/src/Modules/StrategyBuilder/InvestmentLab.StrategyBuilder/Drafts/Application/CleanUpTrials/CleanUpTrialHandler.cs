using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.CleanUpTrials;

public sealed class CleanUpTrialHandler(ITrialCleanupRepository cleanups, IStrategyAuthoring authoring, TimeProvider clock, ILogger<CleanUpTrialHandler> logger)
{
    public async Task Handle(Guid cleanupId, CancellationToken cancellationToken)
    {
        var cleanup = await cleanups.FindAsync(cleanupId, cancellationToken);
        if (cleanup is null)
            return;

        var deleted = await DeleteAsync(cleanup.TrialStrategyId, cancellationToken);
        if (deleted)
            cleanups.Remove(cleanup);
        else
            cleanup.Failed(clock.GetUtcNow());

        await cleanups.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> DeleteAsync(Guid strategyId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authoring.DeleteTrialAsync(strategyId, cancellationToken);
            if (result.IsFailure)
                logger.LogWarning("Trial {StrategyId} cleanup failed: {Code}", strategyId, result.Error.Code);

            return result.IsSuccess;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Trial {StrategyId} cleanup failed", strategyId);

            return false;
        }
    }
}
