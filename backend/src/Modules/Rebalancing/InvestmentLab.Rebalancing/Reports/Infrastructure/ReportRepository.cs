using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Reports.Infrastructure;

internal sealed class ReportRepository(RebalancingDbContext db) : IReportRepository
{
    public Task<RebalancingReport?> FindAsync(ReportId reportId, CancellationToken cancellationToken) =>
        db.Reports.SingleOrDefaultAsync(report => report.Id == reportId, cancellationToken);

    public async Task<IReadOnlyList<RebalancingReport>> ListAsync(Guid? accountId, CancellationToken cancellationToken) =>
        await db.Reports
            .Where(report => accountId == null || report.AccountId == accountId)
            .OrderByDescending(report => report.GeneratedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RebalancingReport>> ForPeriodAsync(Guid accountId, YearMonth period, CancellationToken cancellationToken) =>
        await db.Reports.Where(report => report.AccountId == accountId && report.Period == period).ToListAsync(cancellationToken);

    public async Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken) =>
        await db.Reports
            .Where(report => report.AccountId == accountId && report.Code == code && report.Trigger == ReportTrigger.Scheduled)
            .MaxAsync(report => (DateTimeOffset?)report.GeneratedAt, cancellationToken);

    public async Task<IReadOnlyList<RebalancingReport>> OpenAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Reports
            .Where(report => report.AccountId == accountId && (report.Status == ReportStatus.New || report.Status == ReportStatus.InProgress))
            .ToListAsync(cancellationToken);

    public void Add(RebalancingReport report) => db.Reports.Add(report);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
