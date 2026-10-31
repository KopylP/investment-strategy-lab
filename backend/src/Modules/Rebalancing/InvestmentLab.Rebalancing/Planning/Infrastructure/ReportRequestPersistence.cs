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
        request.Property(r => r.Cause).HasMaxLength(1000);
        request.Property(r => r.SubPortfolioIds).HasJsonConversion();
        request.Property<List<StrategyRun>>("_runs").HasColumnName("runs").HasJsonConversion();
        request.Ignore(r => r.Runs);
        request.Ignore(r => r.IsPending);
        request.Ignore(r => r.IsReady);
        request.Ignore(r => r.DomainEvents);
        request.HasIndex(r => new { r.Status, r.RequestedAt });
        request.HasIndex(r => new { r.AccountId, r.Code });
        request.HasIndex(r => new { r.SignalTriggerId, r.AccountId, r.Code }).IsUnique().HasFilter("signal_trigger_id IS NOT NULL");
        context.HasOwnerFilter(request, r => r.OwnerId);
    }
}
