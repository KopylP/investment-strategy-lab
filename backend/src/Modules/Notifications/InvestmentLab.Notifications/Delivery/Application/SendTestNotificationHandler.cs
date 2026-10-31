using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application;

public sealed class SendTestNotificationHandler(Notifier notifier)
{
    private const string Topic = "test";
    private const string Title = "Investment Lab: тестове сповіщення";
    private const string Message = "Сповіщення працюють: сюди прийде звістка, коли буде готовий рапорт.";

    public async Task<Result> Handle(UserId userId, CancellationToken cancellationToken)
    {
        if (notifier.Channels.Count == 0)
            return NotificationErrors.NoChannel();

        var failures = await notifier.SendAsync(TestNotificationFor(userId), cancellationToken);

        return failures.Count == 0 ? Result.Success() : NotificationErrors.ChannelFailed(string.Join("; ", failures));
    }

    private static Notification TestNotificationFor(UserId userId) =>
        new(userId.Value, Topic, $"investlab_test_{userId.Value:N}", Title, Message, new Dictionary<string, object?>());
}
