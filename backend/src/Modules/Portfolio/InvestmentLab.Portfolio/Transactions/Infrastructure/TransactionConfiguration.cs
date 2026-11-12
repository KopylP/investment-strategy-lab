using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> transaction)
    {
        transaction.ToTable("transactions");
        transaction.HasKey(t => t.Id);
        transaction.Property(t => t.Id).HasConversion<TransactionIdConverter>().ValueGeneratedNever();
        transaction.Property(t => t.AccountId).HasConversion<PortfolioConverters.AccountIdConverter>();
        transaction.Property(t => t.Kind).HasConversion<string>().HasMaxLength(20);
        transaction.Property(t => t.ReversalOf).HasConversion<TransactionIdConverter>();
        transaction.Property(t => t.Details).HasConversion<TransactionDetailsConverter>().HasColumnType("jsonb");
        transaction.OwnsOne(t => t.Source, source =>
        {
            source.Property(s => s.Kind).HasConversion<string>().HasMaxLength(20).HasColumnName("source_kind");
            source.Property(s => s.Reference).HasMaxLength(200).HasColumnName("source_reference");

            source.HasIndex(s => s.Reference).IsUnique().HasFilter("source_reference IS NOT NULL");
        });
        transaction.Navigation(t => t.Source).IsRequired();
        transaction.HasIndex(t => t.ReversalOf).IsUnique().HasFilter("reversal_of IS NOT NULL");
        transaction.HasIndex(t => new { t.AccountId, t.OccurredOn });
        transaction.Ignore(t => t.DomainEvents);
    }
}
