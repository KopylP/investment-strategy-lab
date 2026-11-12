using InvestmentLab.Taxation.Assessments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class LossCarryforwardConfiguration : IEntityTypeConfiguration<LossCarryforward>
{
    public void Configure(EntityTypeBuilder<LossCarryforward> builder)
    {
        builder.ToTable("loss_carryforwards");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => l.OwnerId);
    }
}
