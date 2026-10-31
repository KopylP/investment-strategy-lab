using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Identity.Shared.Infrastructure;

internal sealed class IdentityDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<IdentityDbContext>
{
    protected override string Schema => IdentityDbContext.SchemaName;

    protected override IdentityDbContext Create(DbContextOptions<IdentityDbContext> options) => new(options);
}
