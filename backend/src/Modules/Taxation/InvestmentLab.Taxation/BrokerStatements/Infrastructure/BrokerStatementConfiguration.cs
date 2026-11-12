using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.BrokerStatements.Infrastructure;

internal sealed class BrokerStatementConfiguration : IEntityTypeConfiguration<BrokerStatement>
{
    public void Configure(EntityTypeBuilder<BrokerStatement> builder)
    {
        builder.ToTable("broker_statements");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.AccountId);
        builder.Property(s => s.Broker).HasMaxLength(40);
        builder.Property(s => s.FileName).HasMaxLength(260);
        builder.Property(s => s.Lots).HasJsonConversion();
        builder.Property(s => s.Dividends).HasJsonConversion();
        builder.Property(s => s.Warnings).HasJsonConversion();
        builder.Ignore(s => s.Span);
    }
}
