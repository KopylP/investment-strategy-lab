using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Shared.Infrastructure;

public sealed class StrategiesDbContext(DbContextOptions<StrategiesDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "strategies";

    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyTypeDescriptor> StrategyTypes => Set<StrategyTypeDescriptor>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override Assembly MappingsAssembly => typeof(StrategiesAssemblyMarker).Assembly;
}
