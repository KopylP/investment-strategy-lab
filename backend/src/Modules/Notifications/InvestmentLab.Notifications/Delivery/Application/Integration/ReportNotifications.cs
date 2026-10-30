using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

/// <summary>
/// A report generated on schedule is announced; once it is closed or superseded the announcement is taken back.
/// One the user asked for is not: they are looking at it.
/// </summary>
internal sealed class ReportNotifications(Notifier notifier)
    : IIntegrationEventHandler<ReportGeneratedV1>, IIntegrationEventHandler<ReportClosedV1>, IIntegrationEventHandler<ReportSupersededV1>
{
    public async Task Handle(ReportGeneratedV1 generated, CancellationToken cancellationToken)
    {
        if (ReportNotification.For(generated) is { } notification)
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

    private static readonly string[] Months =
        ["січень", "лютий", "березень", "квітень", "травень", "червень", "липень", "серпень", "вересень", "жовтень", "листопад", "грудень"];

    public static string TagOf(Guid reportId) => $"investlab_report_{reportId:N}";

    /// <summary>Null when it is not worth a notification: asked for by the user, or a risk review with nothing to do.</summary>
    public static Notification? For(ReportGeneratedV1 report)
    {
        if (report.Trigger != "Scheduled")
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
