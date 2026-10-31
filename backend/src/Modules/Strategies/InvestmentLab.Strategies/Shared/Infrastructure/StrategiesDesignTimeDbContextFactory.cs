using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Shared.Infrastructure;

internal sealed class StrategiesDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<StrategiesDbContext>
{
    protected override string Schema => StrategiesDbContext.SchemaName;

    protected override StrategiesDbContext Create(DbContextOptions<StrategiesDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
