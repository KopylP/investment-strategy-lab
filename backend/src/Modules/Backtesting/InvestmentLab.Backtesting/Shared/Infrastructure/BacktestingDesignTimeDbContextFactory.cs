using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Backtesting.Shared.Infrastructure;

internal sealed class BacktestingDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<BacktestingDbContext>
{
    protected override string Schema => BacktestingDbContext.SchemaName;

    protected override BacktestingDbContext Create(DbContextOptions<BacktestingDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
