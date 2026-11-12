using InvestmentLab.Taxation.Ledger.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.Ledger.Infrastructure;

internal sealed class TaxAccountConfiguration : IEntityTypeConfiguration<TaxAccount>
{
    public void Configure(EntityTypeBuilder<TaxAccount> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.Currency).HasMaxLength(3);
        builder.Property(a => a.PolicyCode).HasMaxLength(20);
    }
}
