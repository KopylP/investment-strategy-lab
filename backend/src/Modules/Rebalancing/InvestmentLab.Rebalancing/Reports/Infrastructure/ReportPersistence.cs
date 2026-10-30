using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Reports.Infrastructure;

internal sealed class RebalancingReportConfiguration(RebalancingDbContext context) : IEntityTypeConfiguration<RebalancingReport>
{
    public void Configure(EntityTypeBuilder<RebalancingReport> report)
    {
        report.ToTable("reports");
        report.HasKey(r => r.Id);
        report.Property(r => r.Id).HasConversion(id => id.Value, value => ReportId.Of(value)).ValueGeneratedNever();
        report.Property(r => r.SupersededBy).HasConversion(id => id!.Value.Value, value => ReportId.Of(value));
        report.Property(r => r.AccountName).HasMaxLength(200);
        report.Property(r => r.Kind).HasConversion<string>().HasMaxLength(20);
        report.Property(r => r.Code).HasMaxLength(50);
        report.Property(r => r.Title).HasMaxLength(100);
        report.Property(r => r.Trigger).HasConversion<string>().HasMaxLength(20);
        report.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        report.Property(r => r.Contribution).HasJsonConversion();
        report.Property(r => r.Sections).HasJsonConversion();
        report.Property(r => r.Warnings).HasJsonConversion();
        report.Property(r => r.Review).HasJsonConversion();
        report.Property(r => r.TaxEstimate).HasJsonConversion();
        report.Ignore(r => r.Sales);
        report.HasIndex(r => new { r.AccountId, r.Period });
        report.Ignore(r => r.DomainEvents);
        report.Ignore(r => r.IsOpen);
        report.Ignore(r => r.ContributedAmount);
        context.HasOwnerFilter(report, r => r.OwnerId);

        report.HasMany(r => r.Actions).WithOne().HasForeignKey("ReportId").OnDelete(DeleteBehavior.Cascade);
        report.Navigation(r => r.Actions).HasField("_actions").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

internal sealed class ReportActionConfiguration : IEntityTypeConfiguration<ReportAction>
{
    public void Configure(EntityTypeBuilder<ReportAction> action)
    {
        action.ToTable("report_actions");
        action.HasKey(a => a.Id);
        action.Property(a => a.Id).HasConversion(id => id.Value, value => ReportActionId.Of(value)).ValueGeneratedNever();
        action.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
        action.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
        action.Property(a => a.Reason).HasMaxLength(2000);
        action.Property(a => a.SkipReason).HasMaxLength(500);
        action.Property(a => a.Rejection).HasMaxLength(1000);
        action.Property(a => a.Allocations).HasJsonConversion();
        action.Property(a => a.Fills).HasJsonConversion();
        action.Ignore(a => a.FilledQuantity);
        action.Ignore(a => a.RemainingQuantity);
        action.Ignore(a => a.IsResolved);
        action.Ignore(a => a.IsMarked);
    }
}

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

    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class => db.Publish(integrationEvent, occurredAt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

internal sealed class ReportQueries(IReportRepository reports) : IReportQueries
{
    public async Task<int> GetOpenActionsCount(Guid accountId, CancellationToken cancellationToken) =>
        (await reports.OpenAsync(accountId, cancellationToken)).Sum(report => report.Actions.Count(action => !action.IsResolved));
}
