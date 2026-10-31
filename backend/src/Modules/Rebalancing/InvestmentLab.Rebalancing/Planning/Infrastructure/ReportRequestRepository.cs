using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class ReportRequestRepository(RebalancingDbContext db) : IReportRequestRepository
{
    public Task<ReportRequest?> FindAsync(ReportRequestId requestId, CancellationToken cancellationToken) =>
        db.ReportRequests.SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);

    public async Task<IReadOnlyList<ReportRequest>> PendingAsync(CancellationToken cancellationToken) =>
        await db.ReportRequests
            .Where(request => request.Status == ReportRequestStatus.Pending)
            .OrderBy(request => request.RequestedAt)
            .ToListAsync(cancellationToken);

    public Task<ReportRequest?> PendingAsync(Guid accountId, string code, CancellationToken cancellationToken) =>
        db.ReportRequests
            .Where(request => request.AccountId == accountId && request.Code == code && request.Status == ReportRequestStatus.Pending)
            .OrderByDescending(request => request.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnsweredSignalAsync(Guid signalTriggerId, Guid accountId, string code, CancellationToken cancellationToken) =>
        db.ReportRequests.AnyAsync(request => request.SignalTriggerId == signalTriggerId && request.AccountId == accountId && request.Code == code, cancellationToken);

    public async Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken) =>
        await db.ReportRequests
            .Where(request => request.AccountId == accountId && request.Code == code && request.Trigger == ReportTrigger.Scheduled)
            .MaxAsync(request => (DateTimeOffset?)request.RequestedAt, cancellationToken);

    public async Task<IReadOnlyList<ReportRequest>> RecentAsync(Guid? accountId, DateTimeOffset since, CancellationToken cancellationToken) =>
        await db.ReportRequests
            .Where(request => accountId == null || request.AccountId == accountId)
            .Where(request => request.Status == ReportRequestStatus.Pending || request.CompletedAt >= since)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync(cancellationToken);

    public void Add(ReportRequest request) => db.ReportRequests.Add(request);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
