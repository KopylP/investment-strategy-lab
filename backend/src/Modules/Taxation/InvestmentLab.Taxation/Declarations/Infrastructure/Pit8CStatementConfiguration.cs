using InvestmentLab.Taxation.Declarations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.Declarations.Infrastructure;

internal sealed class Pit8CStatementConfiguration : IEntityTypeConfiguration<Pit8CStatement>
{
    public void Configure(EntityTypeBuilder<Pit8CStatement> builder)
    {
        builder.ToTable("pit8c_statements");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.AccountId, s.Year }).IsUnique();
        builder.Ignore(s => s.Amounts);
    }
}
