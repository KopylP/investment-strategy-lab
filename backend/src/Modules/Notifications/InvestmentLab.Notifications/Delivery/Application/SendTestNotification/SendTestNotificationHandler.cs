using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application.SendTestNotification;

public sealed class SendTestNotificationHandler(Notifier notifier) : IHandler
{
    private const string Topic = "test";
    private static readonly string Title = Message.Of("notification.test.title");
    private static readonly string Body = Message.Of("notification.test.message");
    private const string ProfilePage = "/profile";

    public async Task<Result> Handle(UserId userId, CancellationToken cancellationToken)
    {
        if (notifier.Channels.Count == 0)
            return NotificationErrors.NoChannel();

        var failures = await notifier.SendAsync(TestNotificationFor(userId), cancellationToken);

        return failures.Count == 0 ? Result.Success() : NotificationErrors.ChannelFailed(string.Join("; ", failures));
    }

    private static Notification TestNotificationFor(UserId userId) =>
        new(userId.Value, Topic, $"investlab_test_{userId.Value:N}", Title, Body, ProfilePage, new Dictionary<string, object?>());
}
