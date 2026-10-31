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

    private static readonly string[] Months =
        ["січень", "лютий", "березень", "квітень", "травень", "червень", "липень", "серпень", "вересень", "жовтень", "листопад", "грудень"];

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

        var title = riskReview
            ? $"{report.Title ?? "Перевірка ризику"}: є що зробити"
            : report.Title is { } name ? $"{name} за {PeriodText(report.Period)} готовий" : $"Рапорт за {PeriodText(report.Period)} готовий";
        var account = string.IsNullOrWhiteSpace(report.AccountName) ? "" : $"«{report.AccountName}»: ";
        var message = actions == 0
            ? $"{account}цього разу нічого купувати чи продавати не треба."
            : $"{account}{actions} {ActionsWord(actions)} до виконання.";
        message += NextText(next);

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

        var reason = failed.Error.Length > MaxReasonLength ? $"{failed.Error[..MaxReasonLength]}…" : failed.Error;

        return new Notification(
            failed.OwnerId, FailedTopic, FailedTagOf(failed.AccountId, failed.Code),
            $"{failed.Title} за {PeriodText(failed.Period)} не вдалося підготувати",
            (string.IsNullOrWhiteSpace(reason) ? "Стратегії не відповіли." : $"Причина: {reason}") + NextText(next),
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

    private static string NextText(NextReport? next) => next switch
    {
        null => "",
        { At: { } at } => $" Наступний — {at.ToString("dd.MM.yyyy 'о' HH:mm", CultureInfo.InvariantCulture)}.",
        _ => " Наступний — лише вручну.",
    };

    private static string? NextValue(NextReport? next) => next?.At?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

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
