using System.Globalization;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal static class SignalNotification
{
    public const string Topic = "signal_triggered";

    private const int MaxSubjectsShown = 5;

    public static string TagOf(Guid signalId) => $"investlab_signal_{signalId:N}";

    public static Notification For(SignalTriggeredV1 triggered)
    {
        var title = $"Сигнал: {triggered.Name}";
        var message = MessageOf(triggered.Subjects);
        var data = DataOf(triggered);

        return new Notification(triggered.OwnerId, Topic, TagOf(triggered.SignalId), title, message, data);
    }

    private static string MessageOf(IReadOnlyList<TriggeredSubjectDto> subjects)
    {
        var shown = subjects.Take(MaxSubjectsShown).Select(subject => subject.Text);
        var list = string.Join(", ", shown);
        var hidden = subjects.Count - MaxSubjectsShown;

        return hidden > 0 ? $"{list} і ще {hidden}." : $"{list}.";
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
