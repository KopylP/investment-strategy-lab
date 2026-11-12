using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.StrategyBuilder.Shared.Infrastructure;

public sealed class StrategyBuilderDbContext(DbContextOptions<StrategyBuilderDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "strategy_builder";

    public DbSet<StrategyDraft> Drafts => Set<StrategyDraft>();

    public DbSet<PublishedLayout> PublishedLayouts => Set<PublishedLayout>();
    public DbSet<TrialCleanup> TrialCleanups => Set<TrialCleanup>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override Assembly MappingsAssembly => typeof(StrategyBuilderAssemblyMarker).Assembly;
}
