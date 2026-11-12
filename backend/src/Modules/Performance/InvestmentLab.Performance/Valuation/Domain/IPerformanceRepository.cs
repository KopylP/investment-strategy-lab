using InvestmentLab.Performance.Shadow.Domain;

namespace InvestmentLab.Performance.Valuation.Domain;

public interface IPerformanceRepository
{
    Task<RecalculationMarker> MarkerAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecalculationMarker>> MarkersAsync(CancellationToken cancellationToken);

    Task ReplaceSnapshotsAsync(Guid accountId, IReadOnlyList<ValuationSnapshot> snapshots, CancellationToken cancellationToken);

    Task<IReadOnlyList<ValuationSnapshot>> SnapshotsAsync(
        Guid accountId,
        SnapshotScenario scenario,
        DateOnly from,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlannedReport>> PlannedReportsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<PlannedReport?> FindPlannedReportAsync(Guid reportId, CancellationToken cancellationToken);

    void AddPlannedReport(PlannedReport report);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
