using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.StrategyBuilder.Shared.Infrastructure;

internal sealed class StrategyBuilderDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<StrategyBuilderDbContext>
{
    protected override string Schema => StrategyBuilderDbContext.SchemaName;

    protected override StrategyBuilderDbContext Create(DbContextOptions<StrategyBuilderDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
