using System.Reflection;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyConfiguration(StrategiesDbContext context) : IEntityTypeConfiguration<Strategy>
{
    public void Configure(EntityTypeBuilder<Strategy> strategy)
    {
        strategy.ToTable("strategies");
        strategy.HasKey(s => s.Id);
        strategy.Property(s => s.Id).HasConversion(id => id.Value, value => StrategyId.Of(value)).ValueGeneratedNever();
        strategy.Property(s => s.Name).HasMaxLength(100);
        strategy.Property(s => s.TypeCode).HasMaxLength(100);
        strategy.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        strategy.HasIndex(s => new { s.OwnerId, s.Name }).IsUnique();
        strategy.Ignore(s => s.Latest);
        strategy.Ignore(s => s.DomainEvents);
        context.HasOwnerFilter(strategy, s => s.OwnerId);

        strategy.HasMany(s => s.Versions).WithOne().HasForeignKey("StrategyId").OnDelete(DeleteBehavior.Cascade);
        strategy.Navigation(s => s.Versions).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

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

internal sealed class StrategyRepository(StrategiesDbContext db) : IStrategyRepository
{
    public Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken) =>
        db.Strategies.SingleOrDefaultAsync(strategy => strategy.Id == strategyId, cancellationToken);

    public async Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken cancellationToken) =>
        await db.Strategies.OrderBy(strategy => strategy.CreatedAt).ToListAsync(cancellationToken);

    public Task<bool> NameTakenAsync(UserId ownerId, string name, CancellationToken cancellationToken) =>
        db.Strategies.IgnoreQueryFilters([ModuleDbContext.OwnerFilter]).AnyAsync(strategy => strategy.OwnerId == ownerId && strategy.Name == name, cancellationToken);

    public void Add(Strategy strategy) => db.Strategies.Add(strategy);

    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class => db.Publish(integrationEvent, occurredAt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

internal sealed class StrategyTypes(CatalogSynchronizer synchronizer) : IStrategyTypes
{
    public async Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken) =>
        (await synchronizer.ListAsync(cancellationToken)).FirstOrDefault(type => type.Code == code);
}

internal sealed class DefaultStrategies : IDefaultStrategies
{
    private const string Resource = "InvestmentLab.Strategies.Definitions.Infrastructure.Seed.default-strategies.json";

    public IReadOnlyList<DefaultStrategy> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"Embedded resource {Resource} is missing.");
        return JsonSerializer.Deserialize<List<DefaultStrategy>>(stream, StrategyEngineClient.Json)!;
    }
}
