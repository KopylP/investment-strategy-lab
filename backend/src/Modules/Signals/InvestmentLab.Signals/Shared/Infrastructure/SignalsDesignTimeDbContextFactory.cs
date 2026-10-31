using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Signals.Shared.Infrastructure;

internal sealed class SignalsDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<SignalsDbContext>
{
    protected override string Schema => SignalsDbContext.SchemaName;

    protected override SignalsDbContext Create(DbContextOptions<SignalsDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
