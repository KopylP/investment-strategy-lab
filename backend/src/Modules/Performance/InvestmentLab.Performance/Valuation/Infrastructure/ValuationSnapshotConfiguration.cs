using InvestmentLab.Performance.Valuation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class ValuationSnapshotConfiguration : IEntityTypeConfiguration<ValuationSnapshot>
{
    public void Configure(EntityTypeBuilder<ValuationSnapshot> builder)
    {
        builder.ToTable("valuation_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Scenario).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => new { s.AccountId, s.Scenario, s.Date });
    }
}
