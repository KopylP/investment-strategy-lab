using System.Globalization;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal static class ReportNotification
{
    public const string Topic = "report_ready";

    public const string FailedTopic = "report_failed";

    public static readonly TimeSpan LongWait = TimeSpan.FromMinutes(1);

    private const int MaxReasonLength = 300;

    public static string TagOf(Guid reportId) => $"investlab_report_{reportId:N}";

    public static string FailedTagOf(Guid accountId, string code) => $"investlab_report_failed_{accountId:N}_{code}";

    public static Notification? For(ReportGeneratedV1 report, NextReport? next = null)
    {
        if (!UserIsAway(report.Trigger, report.RequestedAt, report.GeneratedAt))
            return null;

        var actions = report.Actions.Count;
        var riskReview = report.Kind == "RiskReview";
        if (riskReview && actions == 0)
            return null;

        var title = ReadyTitle(report, riskReview);
        var message = WithNext(ReadyBody(report.AccountName, actions), next);

        return new Notification(report.OwnerId, Topic, TagOf(report.ReportId), title, message, new Dictionary<string, object?>
        {
            ["report_id"] = report.ReportId,
            ["account_id"] = report.AccountId,
            ["account_name"] = report.AccountName,
            ["kind"] = report.Kind,
            ["period"] = report.Period,
            ["actions"] = actions,
            ["next"] = NextValue(next),
        });
    }

    public static Notification? For(ReportRequestFailedV1 failed, NextReport? next = null)
    {
        if (!UserIsAway(failed.Trigger, failed.RequestedAt, failed.FailedAt))
            return null;

        var reason = ShortReason(failed.Error);
        var body = string.IsNullOrWhiteSpace(reason) ? Message.Of("notification.report.failed_no_reason") : Message.Of("notification.report.failed_reason", new { reason });

        return new Notification(
            failed.OwnerId, FailedTopic, FailedTagOf(failed.AccountId, failed.Code),
            Message.Of("notification.report.failed_title", new { title = failed.Title, period = PeriodText(failed.Period) }),
            WithNext(body, next),
            new Dictionary<string, object?>
            {
                ["request_id"] = failed.RequestId,
                ["account_id"] = failed.AccountId,
                ["code"] = failed.Code,
                ["kind"] = failed.Kind,
                ["period"] = failed.Period,
                ["next"] = NextValue(next),
            });
    }

    private static bool UserIsAway(string? trigger, DateTimeOffset? requestedAt, DateTimeOffset doneAt) =>
        trigger is "Scheduled" or "Signal" || (trigger == "OnDemand" && requestedAt is { } asked && doneAt - asked >= LongWait);

    private static string ReadyTitle(ReportGeneratedV1 report, bool riskReview)
    {
        var period = PeriodText(report.Period);

        if (riskReview)
            return Message.Of("notification.report.risk_title", new { title = report.Title ?? Message.Of("report.title.risk_review") });

        return report.Title is { } title
            ? Message.Of("notification.report.named_title", new { title, period })
            : Message.Of("notification.report.unnamed_title", new { period });
    }

    private static string ReadyBody(string? accountName, int actions)
    {
        var account = string.IsNullOrWhiteSpace(accountName) ? "" : Message.Of("notification.report.account_prefix", new { name = accountName });

        return actions == 0
            ? Message.Of("notification.report.nothing_to_do", new { account })
            : Message.Of("notification.report.actions", new { account, count = actions });
    }

    private static string ShortReason(string reason)
    {
        if (reason.StartsWith(Message.Prefix) || reason.Length <= MaxReasonLength)
            return reason;

        return $"{reason[..MaxReasonLength]}…";
    }

    private static string WithNext(string body, NextReport? next) => next switch
    {
        null => body,
        { At: { } at } => Message.Of("notification.with_next", new { body, next = NextAt(at) }),
        _ => Message.Of("notification.with_next", new { body, next = Message.Of("notification.next.manual") }),
    };

    private static string NextAt(DateTimeOffset at) => Message.Of("notification.next.at", new
    {
        date = at.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        time = at.ToString("HH:mm", CultureInfo.InvariantCulture),
    });

    private static string? NextValue(NextReport? next) => next?.At?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    private static string PeriodText(string period)
    {
        var month = YearMonth.TryParse(period);
        if (month.IsFailure)
            return period;

        return Message.Of("notification.period", new { month = Message.Of($"month.{month.Value.Month}"), year = month.Value.Year });
    }
}
