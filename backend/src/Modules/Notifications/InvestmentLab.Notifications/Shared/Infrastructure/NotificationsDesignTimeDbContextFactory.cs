using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Notifications.Shared.Infrastructure;

internal sealed class NotificationsDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<NotificationsDbContext>
{
    protected override string Schema => NotificationsDbContext.SchemaName;

    protected override NotificationsDbContext Create(DbContextOptions<NotificationsDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
