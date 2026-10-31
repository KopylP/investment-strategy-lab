using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Shared.Infrastructure;
using InvestmentLab.Performance.Valuation.Application;
using InvestmentLab.Performance.Valuation.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class PerformanceRepository(PerformanceDbContext db) : IPerformanceRepository
{
    public async Task<RecalculationMarker> MarkerAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken)
    {
        if (await db.Markers.IgnoreQueryFilters().SingleOrDefaultAsync(marker => marker.AccountId == accountId, cancellationToken) is { } existing)
            return existing;

        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO performance.recalculation_markers (account_id, owner_id, dirty_from) VALUES ({accountId}, {ownerId.Value}, {DateOnly.MinValue}) ON CONFLICT (account_id) DO NOTHING",
            cancellationToken);
        return await db.Markers.IgnoreQueryFilters().SingleAsync(marker => marker.AccountId == accountId, cancellationToken);
    }

    public async Task<IReadOnlyList<RecalculationMarker>> MarkersAsync(CancellationToken cancellationToken) => await db.Markers.ToListAsync(cancellationToken);

    public Task ReplaceSnapshotsAsync(Guid accountId, IReadOnlyList<ValuationSnapshot> snapshots, CancellationToken cancellationToken) =>

        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var lockKey = BitConverter.ToInt64(accountId.ToByteArray(), 0);
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);
            await db.Snapshots.Where(snapshot => snapshot.AccountId == accountId).ExecuteDeleteAsync(cancellationToken);
            db.Snapshots.AddRange(snapshots);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

    public async Task<IReadOnlyList<ValuationSnapshot>> SnapshotsAsync(
        Guid accountId,
        SnapshotScenario scenario,
        DateOnly from,
        CancellationToken cancellationToken) =>
        await db.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.AccountId == accountId && snapshot.Scenario == scenario && snapshot.Date >= from)
            .OrderBy(snapshot => snapshot.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlannedReport>> PlannedReportsAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.PlannedReports.Where(report => report.AccountId == accountId).ToListAsync(cancellationToken);

    public Task<PlannedReport?> FindPlannedReportAsync(Guid reportId, CancellationToken cancellationToken) =>
        db.PlannedReports.SingleOrDefaultAsync(report => report.ReportId == reportId, cancellationToken);

    public void AddPlannedReport(PlannedReport report) => db.PlannedReports.Add(report);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
