using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

public sealed class RebuildSnapshotsJob(IPerformanceRepository store, SnapshotBuilder builder, ILogger<RebuildSnapshotsJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = DailySchedule.Weekdays(new TimeOnly(23, 15), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var marker in (await store.MarkersAsync(cancellationToken)).Where(marker => marker.IsDirty).ToList())
        {
            var rebuilt = await builder.EnsureFreshAsync(marker.AccountId, cancellationToken);
            if (rebuilt.IsFailure)
                logger.LogWarning("Snapshots of account {AccountId} could not be rebuilt: {Error}", marker.AccountId, rebuilt.Error.Message);
        }
    }
}
