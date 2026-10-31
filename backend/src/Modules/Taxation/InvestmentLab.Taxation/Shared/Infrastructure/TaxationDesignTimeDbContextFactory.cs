using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.Shared.Infrastructure;

internal sealed class TaxationDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<TaxationDbContext>
{
    protected override string Schema => TaxationDbContext.SchemaName;

    protected override TaxationDbContext Create(DbContextOptions<TaxationDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
