using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Performance.Shared.Infrastructure;

internal sealed class PerformanceDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<PerformanceDbContext>
{
    protected override string Schema => PerformanceDbContext.SchemaName;

    protected override PerformanceDbContext Create(DbContextOptions<PerformanceDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
