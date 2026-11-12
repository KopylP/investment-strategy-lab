using System.Globalization;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal static class SignalNotification
{
    public const string Topic = "signal_triggered";

    private const int MaxSubjectsShown = 5;

    private const string SignalsPage = "/signals";

    public static string TagOf(Guid signalId) => $"investlab_signal_{signalId:N}";

    public static Notification For(SignalTriggeredV1 triggered)
    {
        var title = Message.Of("notification.signal.title", new { name = triggered.Name });
        var message = MessageOf(triggered.Subjects);
        var data = DataOf(triggered);

        return new Notification(triggered.OwnerId, Topic, TagOf(triggered.SignalId), title, message, SignalsPage, data);
    }

    private static string MessageOf(IReadOnlyList<TriggeredSubjectDto> subjects)
    {
        var list = subjects.Take(MaxSubjectsShown).Select(subject => subject.Text).ToList();
        var hidden = subjects.Count - MaxSubjectsShown;

        return hidden > 0
            ? Message.Of("notification.signal.more", new { list, hidden })
            : Message.Of("notification.signal.list", new { list });
    }

    private static Dictionary<string, object?> DataOf(SignalTriggeredV1 triggered) => new()
    {
        ["signal_id"] = triggered.SignalId,
        ["account_id"] = triggered.AccountId,
        ["metric"] = triggered.Metric,
        ["data_date"] = triggered.DataDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["subjects"] = triggered.Subjects.Count,
    };
}
