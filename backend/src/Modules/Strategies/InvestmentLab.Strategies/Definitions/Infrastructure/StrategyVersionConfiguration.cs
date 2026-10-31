using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Definitions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyVersionConfiguration : IEntityTypeConfiguration<StrategyVersion>
{
    public void Configure(EntityTypeBuilder<StrategyVersion> version)
    {
        version.ToTable("strategy_versions");
        version.HasKey(v => v.Id);
        version.Property(v => v.Id).ValueGeneratedNever();
        version.Property(v => v.Parameters).HasColumnType("jsonb");
        version.HasIndex("StrategyId", nameof(StrategyVersion.Number)).IsUnique();
    }
}
