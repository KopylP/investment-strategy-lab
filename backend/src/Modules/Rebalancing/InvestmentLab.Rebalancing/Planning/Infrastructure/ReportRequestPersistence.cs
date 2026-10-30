using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class ReportRequestConfiguration(RebalancingDbContext context) : IEntityTypeConfiguration<ReportRequest>
{
    public void Configure(EntityTypeBuilder<ReportRequest> request)
    {
        request.ToTable("report_requests");
        request.HasKey(r => r.Id);
        request.Property(r => r.Id).HasConversion(id => id.Value, value => ReportRequestId.Of(value)).ValueGeneratedNever();
        request.Property(r => r.ReportId).HasConversion(id => id!.Value.Value, value => ReportId.Of(value));
        request.Property(r => r.Code).HasMaxLength(50);
        request.Property(r => r.Title).HasMaxLength(100);
        request.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);
        request.Property(r => r.Trigger).HasConversion<string>().HasMaxLength(20);
        request.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        request.Property(r => r.Error).HasMaxLength(2000);
        request.Property(r => r.SubPortfolioIds).HasJsonConversion();
        request.Property<List<StrategyRun>>("_runs").HasColumnName("runs").HasJsonConversion();
        request.Ignore(r => r.Runs);
        request.Ignore(r => r.IsPending);
        request.Ignore(r => r.IsReady);
        request.Ignore(r => r.DomainEvents);
        request.HasIndex(r => new { r.Status, r.RequestedAt });
        request.HasIndex(r => new { r.AccountId, r.Code });
        context.HasOwnerFilter(request, r => r.OwnerId);
    }
}

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
