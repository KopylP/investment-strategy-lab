using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application.GetNotificationSettings;

public sealed record DestinationDto(string Id, string Channel, string Label, bool Suggested, bool Selected);

public sealed record NotificationSettingsDto(IReadOnlyList<string> Channels, bool Automatic, IReadOnlyList<DestinationDto> Destinations);

public sealed class GetNotificationSettingsHandler(Notifier notifier) : IHandler
{
    public async Task<Result<NotificationSettingsDto>> Handle(UserId userId, CancellationToken cancellationToken)
    {
        var recipient = await notifier.RecipientAsync(userId.Value, cancellationToken);
        var destinations = new List<DestinationDto>();

        foreach (var channel in notifier.Channels)
        {
            IReadOnlyList<Destination> available;

            try
            {
                available = await channel.DestinationsAsync(recipient, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                return NotificationErrors.ChannelFailed(channel.Name, exception.Message);
            }

            var targets = recipient.Targets(available).Select(destination => destination.Id).ToHashSet();
            destinations.AddRange(available.Select(destination =>
                new DestinationDto(destination.Id, channel.Name, destination.Label, destination.Suggested, targets.Contains(destination.Id))));
        }

        return new NotificationSettingsDto(
            [.. notifier.Channels.Select(channel => channel.Name)],
            recipient.ChosenDestinations is null,
            destinations);
    }
}
