using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Notifications.Shared.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "notifications";

    public DbSet<NotificationPreferences> Preferences => Set<NotificationPreferences>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new NotificationPreferencesConfiguration(this));
}
