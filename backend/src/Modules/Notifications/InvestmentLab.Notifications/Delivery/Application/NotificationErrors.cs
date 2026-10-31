namespace InvestmentLab.Notifications.Delivery.Application;

public static class NotificationErrors
{
    public static Error NoChannel() =>
        Error.Conflict("notification.no_channel", "No notification channel is configured: the app does not run in Home Assistant.");

    public static Error ChannelFailed(string details) => Error.Conflict("notification.channel_failed", $"The notification was not delivered: {details}");

    public static Error ChannelFailed(string channel, string details) => ChannelFailed($"{channel}: {details}");
}
