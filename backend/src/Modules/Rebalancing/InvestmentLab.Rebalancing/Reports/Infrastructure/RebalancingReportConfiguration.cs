using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Reports.Infrastructure;

internal sealed class RebalancingReportConfiguration : IEntityTypeConfiguration<RebalancingReport>
{
    private const string ConcurrencyVersion = "Version";

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
        report.Property<uint>(ConcurrencyVersion).IsRowVersion();
        report.HasIndex(r => new { r.AccountId, r.Period });
        report.Ignore(r => r.DomainEvents);
        report.Ignore(r => r.IsOpen);
        report.Ignore(r => r.ContributedAmount);

        report.OwnsMany(r => r.Actions, ReportActionMapping.Configure);
        report.Navigation(r => r.Actions).HasField("_actions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
