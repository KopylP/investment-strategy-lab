using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Performance.Shadow.Infrastructure;

internal sealed class PlannedReportConfiguration : IEntityTypeConfiguration<PlannedReport>
{
    public void Configure(EntityTypeBuilder<PlannedReport> builder)
    {
        builder.ToTable("planned_reports");
        builder.HasKey(r => r.ReportId);
        builder.Property(r => r.Kind).HasMaxLength(20);
        builder.Property(r => r.Period).HasMaxLength(7);
        builder.Property(r => r.Actions).HasJsonConversion();
        builder.HasIndex(r => r.AccountId);
        builder.Ignore(r => r.CountsForPlan);
    }
}
