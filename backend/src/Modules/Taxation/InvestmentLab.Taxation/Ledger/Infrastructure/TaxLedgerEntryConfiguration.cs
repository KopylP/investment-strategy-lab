using InvestmentLab.Taxation.Ledger.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.Ledger.Infrastructure;

internal sealed class TaxLedgerEntryConfiguration : IEntityTypeConfiguration<TaxLedgerEntry>
{
    public void Configure(EntityTypeBuilder<TaxLedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(e => e.TransactionId);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Currency).HasMaxLength(3);
        builder.Property(e => e.Allocations).HasJsonConversion();
        builder.HasIndex(e => e.AccountId);
        builder.Ignore(e => e.TaxYear);
    }
}
