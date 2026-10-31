using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Shared.Infrastructure;

internal sealed class PortfolioDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<PortfolioDbContext>
{
    protected override string Schema => PortfolioDbContext.SchemaName;

    protected override PortfolioDbContext Create(DbContextOptions<PortfolioDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
