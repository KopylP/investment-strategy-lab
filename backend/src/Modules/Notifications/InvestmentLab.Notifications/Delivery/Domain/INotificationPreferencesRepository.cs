namespace InvestmentLab.Notifications.Delivery.Domain;

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> FindAsync(UserId ownerId, CancellationToken cancellationToken);

    void Add(NotificationPreferences preferences);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
