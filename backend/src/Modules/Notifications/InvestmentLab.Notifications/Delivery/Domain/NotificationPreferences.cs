namespace InvestmentLab.Notifications.Delivery.Domain;

/// <summary>Where a user wants their notifications pushed. Without it (or with no choice) the channels suggest.</summary>
public sealed class NotificationPreferences : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }

    /// <summary>Null: automatic, whatever each channel suggests for the user.</summary>
    public List<string>? Destinations { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private NotificationPreferences(Guid id, UserId ownerId) : base(id) => OwnerId = ownerId;

    // EF Core.
    private NotificationPreferences(Guid id) : base(id)
    {
    }

    public static NotificationPreferences For(UserId ownerId) => new(ownerId.Value, ownerId);

    public void Choose(IEnumerable<string>? destinations, DateTimeOffset now)
    {
        Destinations = destinations?.Distinct().ToList();
        UpdatedAt = now;
    }
}

public interface INotificationPreferencesRepository
{
    Task<NotificationPreferences?> FindAsync(UserId ownerId, CancellationToken cancellationToken);

    void Add(NotificationPreferences preferences);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
