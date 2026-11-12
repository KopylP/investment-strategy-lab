using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class TaxAssessmentConfiguration : IEntityTypeConfiguration<TaxAssessment>
{
    public void Configure(EntityTypeBuilder<TaxAssessment> builder)
    {
        builder.Ignore(a => a.DomainEvents);
        builder.ToTable("assessments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.OwnerId, a.Year }).IsUnique();
        builder.Property(a => a.PolicyCode).HasMaxLength(20);
        builder.Property(a => a.InputHash).HasMaxLength(64);
        builder.Property(a => a.Summary).HasJsonConversion();
        builder.Property(a => a.Realizations).HasJsonConversion();
        builder.Property(a => a.Dividends).HasJsonConversion();
        builder.Property(a => a.CarryforwardUses).HasJsonConversion();
    }
}
