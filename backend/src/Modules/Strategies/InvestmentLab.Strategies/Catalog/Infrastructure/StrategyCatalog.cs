using InvestmentLab.Strategies.Catalog.Application;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Catalog.Infrastructure;

internal sealed class StrategyCatalog(CatalogSynchronizer synchronizer) : IStrategyCatalog
{
    public Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken) => synchronizer.ListAsync(cancellationToken);

    public Task<int> SyncAsync(CancellationToken cancellationToken) => synchronizer.SyncAsync(cancellationToken);
}

internal sealed class StrategyTypeConfiguration : IEntityTypeConfiguration<StrategyTypeDescriptor>
{
    public void Configure(EntityTypeBuilder<StrategyTypeDescriptor> type)
    {
        type.ToTable("strategy_types");
        type.HasKey(t => t.Code);
        type.Property(t => t.Code).HasMaxLength(100);
        type.Property(t => t.Name).HasMaxLength(200);
        type.Property(t => t.Engine).HasMaxLength(50);
        type.Property(t => t.EngineVersion).HasMaxLength(100);
        type.Property(t => t.ParametersSchema).HasColumnType("jsonb");
        type.Property(t => t.DefaultParameters).HasColumnType("jsonb");
    }
}
