using System.Reflection;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Experiments.Infrastructure;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Backtesting.Shared.Infrastructure;

public sealed class BacktestingDbContext(DbContextOptions<BacktestingDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "backtesting";

    public DbSet<Backtest> Backtests => Set<Backtest>();
    public DbSet<ResearchExperiment> Experiments => Set<ResearchExperiment>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override Assembly MappingsAssembly => typeof(BacktestingAssemblyMarker).Assembly;
}
