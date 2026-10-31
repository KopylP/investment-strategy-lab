namespace InvestmentLab.Notifications.Delivery.Domain;

public sealed class NotificationPreferences : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }

    public List<string>? Destinations { get; private set; }

    public List<Guid> MutedSignalIds { get; private set; } = [];

    public DateTimeOffset UpdatedAt { get; private set; }

    private NotificationPreferences(Guid id, UserId ownerId) : base(id) => OwnerId = ownerId;

    private NotificationPreferences(Guid id) : base(id)
    {
    }

    public static NotificationPreferences For(UserId ownerId) => new(ownerId.Value, ownerId);

    public bool Hears(Guid signalId) => !MutedSignalIds.Contains(signalId);

    public void HearSignal(Guid signalId, bool notify, DateTimeOffset now)
    {
        MutedSignalIds.Remove(signalId);

        if (!notify)
            MutedSignalIds.Add(signalId);

        UpdatedAt = now;
    }

    public void Choose(IEnumerable<string>? destinations, DateTimeOffset now)
    {
        Destinations = destinations?.Distinct().ToList();
        UpdatedAt = now;
    }
}
