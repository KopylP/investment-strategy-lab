using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Reports.Infrastructure;

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
