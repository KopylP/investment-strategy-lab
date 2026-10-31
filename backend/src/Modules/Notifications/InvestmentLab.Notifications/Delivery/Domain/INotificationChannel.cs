namespace InvestmentLab.Notifications.Delivery.Domain;

public interface INotificationChannel
{
    string Name { get; }

    Task<IReadOnlyList<Destination>> DestinationsAsync(Recipient recipient, CancellationToken cancellationToken);

    Task SendAsync(Notification notification, Recipient recipient, CancellationToken cancellationToken);

    Task DismissAsync(string tag, CancellationToken cancellationToken);
}
