using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

internal sealed class NotificationPreferencesRepository(NotificationsDbContext db) : INotificationPreferencesRepository
{
    public Task<NotificationPreferences?> FindAsync(UserId ownerId, CancellationToken cancellationToken) =>
        db.Preferences.SingleOrDefaultAsync(preferences => preferences.Id == ownerId.Value, cancellationToken);

    public void Add(NotificationPreferences preferences) => db.Preferences.Add(preferences);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
