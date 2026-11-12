using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

internal sealed class HoldingRowConfiguration : IEntityTypeConfiguration<HoldingRow>
{
    public void Configure(EntityTypeBuilder<HoldingRow> row)
    {
        row.ToTable("holdings");
        row.HasKey(r => r.Id);
        row.Property(r => r.Lots).HasColumnType("jsonb");
        row.HasIndex(r => new { r.AccountId, r.SubPortfolioId, r.InstrumentId, r.Currency }).IsUnique().AreNullsDistinct(false);
    }
}
