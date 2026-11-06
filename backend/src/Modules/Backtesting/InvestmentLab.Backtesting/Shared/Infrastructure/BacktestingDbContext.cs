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

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Backtest>(backtest =>
        {
            BacktestConfiguration.Configure(backtest);
            HasOwnerFilter(backtest, entry => entry.OwnerId);
        });
        modelBuilder.Entity<ResearchExperiment>(experiment =>
        {
            ExperimentConfiguration.Configure(experiment);
            HasOwnerFilter(experiment, entry => entry.OwnerId);
        });
    }
}
