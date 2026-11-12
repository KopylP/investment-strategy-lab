using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

public sealed class RebuildSnapshotsJob(IPerformanceRepository store, SnapshotBuilder builder) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = DailySchedule.Weekdays(new TimeOnly(23, 15), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var marker in (await store.MarkersAsync(cancellationToken)).Where(marker => marker.IsDirty).ToList())
            await builder.EnsureFreshAsync(marker.AccountId, cancellationToken);
    }
}
