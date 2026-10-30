using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

/// <summary>
/// A report generated on schedule is announced; once it is closed or superseded the announcement is taken back.
/// One the user asked for is announced only when its strategies thought long enough for them to have left the page.
/// A report that could not be made is announced the same way, with why.
/// </summary>
internal sealed class ReportNotifications(Notifier notifier)
    : IIntegrationEventHandler<ReportGeneratedV1>, IIntegrationEventHandler<ReportClosedV1>, IIntegrationEventHandler<ReportSupersededV1>,
      IIntegrationEventHandler<ReportRequestFailedV1>
{
    public async Task Handle(ReportGeneratedV1 generated, CancellationToken cancellationToken)
    {
        if (ReportNotification.For(generated) is { } notification)
            await notifier.SendAsync(notification, cancellationToken);
    }

    public async Task Handle(ReportRequestFailedV1 failed, CancellationToken cancellationToken)
    {
        if (ReportNotification.For(failed) is { } notification)
            await notifier.SendAsync(notification, cancellationToken);
    }

    public Task Handle(ReportClosedV1 closed, CancellationToken cancellationToken) =>
        notifier.DismissAsync(ReportNotification.TagOf(closed.ReportId), cancellationToken);

    public Task Handle(ReportSupersededV1 superseded, CancellationToken cancellationToken) =>
        notifier.DismissAsync(ReportNotification.TagOf(superseded.ReportId), cancellationToken);
}

internal static class ReportNotification
{
    public const string Topic = "report_ready";

    public const string FailedTopic = "report_failed";

    /// <summary>A report the user asked for that took this long is announced: they have likely left the page by then.</summary>
    public static readonly TimeSpan LongWait = TimeSpan.FromMinutes(1);

    private const int MaxReasonLength = 300;

    private static readonly string[] Months =
        ["січень", "лютий", "березень", "квітень", "травень", "червень", "липень", "серпень", "вересень", "жовтень", "листопад", "грудень"];

    public static string TagOf(Guid reportId) => $"investlab_report_{reportId:N}";

    /// <summary>A failure of the report with this code is replaced by the next one, not stacked.</summary>
    public static string FailedTagOf(Guid accountId, string code) => $"investlab_report_failed_{accountId:N}_{code}";

    /// <summary>
    /// Null when it is not worth a notification: asked for by the user and made while they were likely still looking,
    /// or a risk review with nothing to do.
    /// </summary>
    public static Notification? For(ReportGeneratedV1 report)
    {
        if (!UserIsAway(report.Trigger, report.RequestedAt, report.GeneratedAt))
            return null;
        var actions = report.Actions.Count;
        var riskReview = report.Kind == "RiskReview";
        if (riskReview && actions == 0)
            return null;

        var title = riskReview
            ? $"{report.Title ?? "Перевірка ризику"}: є що зробити"
            : report.Title is { } name ? $"{name} за {PeriodText(report.Period)} готовий" : $"Рапорт за {PeriodText(report.Period)} готовий";
        var account = string.IsNullOrWhiteSpace(report.AccountName) ? "" : $"«{report.AccountName}»: ";
        var message = actions == 0
            ? $"{account}цього разу нічого купувати чи продавати не треба."
            : $"{account}{actions} {ActionsWord(actions)} до виконання.";

        return new Notification(report.OwnerId, Topic, TagOf(report.ReportId), title, message, new Dictionary<string, object?>
        {
            ["report_id"] = report.ReportId,
            ["account_id"] = report.AccountId,
            ["account_name"] = report.AccountName,
            ["kind"] = report.Kind,
            ["period"] = report.Period,
            ["actions"] = actions,
        });
    }

    /// <summary>Null when the user asked for it a moment ago: the app shows them why.</summary>
    public static Notification? For(ReportRequestFailedV1 failed)
    {
        if (!UserIsAway(failed.Trigger, failed.RequestedAt, failed.FailedAt))
            return null;

        var reason = failed.Error.Length > MaxReasonLength ? $"{failed.Error[..MaxReasonLength]}…" : failed.Error;
        return new Notification(
            failed.OwnerId, FailedTopic, FailedTagOf(failed.AccountId, failed.Code),
            $"{failed.Title} за {PeriodText(failed.Period)} не вдалося підготувати",
            string.IsNullOrWhiteSpace(reason) ? "Стратегії не відповіли." : $"Причина: {reason}",
            new Dictionary<string, object?>
            {
                ["request_id"] = failed.RequestId,
                ["account_id"] = failed.AccountId,
                ["code"] = failed.Code,
                ["kind"] = failed.Kind,
                ["period"] = failed.Period,
            });
    }

    /// <summary>A scheduled report always finds the user away; one they asked for, when it took at least <see cref="LongWait"/>.</summary>
    private static bool UserIsAway(string? trigger, DateTimeOffset? requestedAt, DateTimeOffset doneAt) =>
        trigger == "Scheduled" || (trigger == "OnDemand" && requestedAt is { } asked && doneAt - asked >= LongWait);

    private static string PeriodText(string period)
    {
        var month = YearMonth.TryParse(period);
        return month.IsSuccess ? $"{Months[month.Value.Month - 1]} {month.Value.Year}" : period;
    }

    private static string ActionsWord(int count) => (count % 100, count % 10) switch
    {
        ( >= 11 and <= 14, _) => "дій",
        (_, 1) => "дія",
        (_, >= 2 and <= 4) => "дії",
        _ => "дій",
    };
}
