using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Shared.Infrastructure;

internal sealed class RebalancingDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<RebalancingDbContext>
{
    protected override string Schema => RebalancingDbContext.SchemaName;

    protected override RebalancingDbContext Create(DbContextOptions<RebalancingDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
