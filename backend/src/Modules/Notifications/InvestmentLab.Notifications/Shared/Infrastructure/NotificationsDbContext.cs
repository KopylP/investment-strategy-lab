using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Notifications.Delivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Notifications.Shared.Infrastructure;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "notifications";

    public DbSet<NotificationPreferences> Preferences => Set<NotificationPreferences>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override Assembly MappingsAssembly => typeof(NotificationsAssemblyMarker).Assembly;
}
