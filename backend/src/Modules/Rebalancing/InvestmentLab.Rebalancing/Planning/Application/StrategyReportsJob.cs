using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class StrategyReportsJob(
    PlanningInputs inputs, IPlanSettingsRepository settings, IReportRepository reports, IReportRequestRepository requests,
    RequestReportHandler request, TimeProvider clock, ILogger<StrategyReportsJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
            await AskForDueReportsAsync(account, now, cancellationToken);
    }

    private async Task AskForDueReportsAsync(AccountSetupDto account, DateTime now, CancellationToken cancellationToken)
    {
        var plan = await settings.FindAsync(account.Id, cancellationToken);
        if (plan is { GenerateOnSchedule: false })
            return;

        foreach (var report in await inputs.ReportsAsync(account, plan, cancellationToken))
            await AskIfDueAsync(account.Id, report, now, cancellationToken);
    }

    private async Task AskIfDueAsync(Guid accountId, AccountReport report, DateTime now, CancellationToken cancellationToken)
    {
        var times = RecentReportTimes(report, now);
        if (times.Count == 0)
            return;

        var answeredUntil = await AnsweredUntilAsync(accountId, report, cancellationToken);
        var due = times
            .Where(each => each.Time > answeredUntil)
            .Select(each => each.SubPortfolioId)
            .ToHashSet();
        if (due.Count == 0)
            return;

        var today = DateOnly.FromDateTime(now);
        if (await IsMonthlyReportOpenAsync(accountId, report, today, cancellationToken))
            return;

        await AskAsync(accountId, report, today, due, cancellationToken);
    }

    private static List<(Guid SubPortfolioId, DateTime? Time)> RecentReportTimes(AccountReport report, DateTime now)
    {
        var times = report.SubPortfolios.Select(sub => (sub.SubPortfolioId, Time: report.CadenceOf(sub).Last(now)));

        return [.. times.Where(each => each.Time is { } time && now - time < ReportSchedules.CatchUp)];
    }

    private async Task<DateTime> AnsweredUntilAsync(Guid accountId, AccountReport report, CancellationToken cancellationToken)
    {
        var lastReport = await reports.LastScheduledAsync(accountId, report.Code, cancellationToken);
        var lastRequest = await requests.LastScheduledAsync(accountId, report.Code, cancellationToken);
        var lastAsked = lastRequest > lastReport ? lastRequest : lastReport ?? lastRequest;

        var askedAt = lastAsked is { } at ? ReportSchedules.Local(at).DateTime : DateTime.MinValue;
        var ownScheduleSince = report.Own is { } own ? ReportSchedules.Local(own.Since).DateTime : DateTime.MinValue;

        return ownScheduleSince > askedAt ? ownScheduleSince : askedAt;
    }

    private async Task<bool> IsMonthlyReportOpenAsync(Guid accountId, AccountReport report, DateOnly today, CancellationToken cancellationToken)
    {
        if (report.Kind != ReportKind.Monthly)
            return false;

        var ofMonth = await reports.ForPeriodAsync(accountId, YearMonth.Containing(today), cancellationToken);

        return ofMonth.Any(each => each.Code == report.Code && each.IsOpen);
    }

    private async Task AskAsync(Guid accountId, AccountReport report, DateOnly today, HashSet<Guid> due, CancellationToken cancellationToken)
    {
        var command = new RequestReportCommand(accountId, report.Code, AsOf: today, SubPortfolioIds: due);

        var asked = await request.Handle(command, ReportTrigger.Scheduled, cancellationToken);
        if (asked.IsFailure)
            logger.LogWarning("Report {Code} for account {AccountId} was not asked for: {Error}", report.Code, accountId, asked.Error.Message);
    }
}
