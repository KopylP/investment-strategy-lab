using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Catalog.Infrastructure;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Definitions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Shared.Infrastructure;

public sealed class StrategiesDbContext(DbContextOptions<StrategiesDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "strategies";

    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyTypeDescriptor> StrategyTypes => Set<StrategyTypeDescriptor>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StrategyTypeConfiguration());
        modelBuilder.ApplyConfiguration(new StrategyConfiguration(this));
        modelBuilder.ApplyConfiguration(new StrategyVersionConfiguration());
    }
}

internal sealed class StrategiesDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<StrategiesDbContext>
{
    protected override string Schema => StrategiesDbContext.SchemaName;

    protected override StrategiesDbContext Create(DbContextOptions<StrategiesDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
