using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyConfiguration : IEntityTypeConfiguration<Strategy>
{
    public void Configure(EntityTypeBuilder<Strategy> strategy)
    {
        strategy.ToTable("strategies");
        strategy.HasKey(s => s.Id);
        strategy.Property(s => s.Id).HasConversion(id => id.Value, value => StrategyId.Of(value)).ValueGeneratedNever();
        strategy.Property(s => s.Name).HasMaxLength(100);
        strategy.Property(s => s.DeletedSignalIds).HasColumnType("uuid[]");
        strategy.Property(s => s.TypeCode).HasMaxLength(100);
        strategy.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        strategy.HasIndex(s => new { s.OwnerId, s.Name }).IsUnique();
        strategy.Ignore(s => s.Latest);
        strategy.Ignore(s => s.DomainEvents);

        strategy.HasMany(s => s.Versions).WithOne().HasForeignKey("StrategyId").OnDelete(DeleteBehavior.Cascade);
        strategy.Navigation(s => s.Versions).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
