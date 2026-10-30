using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Rebalancing.Planning.Application.GenerateReport;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application;

public static class ReportSchedules
{
    public const string TimeZone = "Europe/Warsaw";

    /// <summary>The job looks this often whether a report's time has come: a report declared for 20:00 comes by 20:15.</summary>
    public static readonly IntervalSchedule Check = new(TimeSpan.FromMinutes(15));

    public static DateTimeOffset Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(Local(clock.GetUtcNow()).DateTime);
}

/// <summary>
/// Makes every report the accounts' strategies declare once its day and time have come: for the sub-portfolios whose
/// strategy has it due today, once that day. A rebalance report is not made while one of the same code is still open
/// for the month (D14: a user's report is never overwritten automatically).
/// </summary>
public sealed class StrategyReportsJob(
    PlanningInputs inputs, IPlanSettingsRepository settings, IReportRepository reports, GenerateReportHandler generate,
    TimeProvider clock, ILogger<StrategyReportsJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = ReportSchedules.Local(clock.GetUtcNow());
        var today = DateOnly.FromDateTime(now.DateTime);
        var time = TimeOnly.FromDateTime(now.DateTime);
        if (today.DayOfWeek != DayOfWeek.Sunday)
            return;

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
        {
            if (await settings.FindAsync(account.Id, cancellationToken) is { GenerateOnSchedule: false })
                continue;

            var made = await reports.ForPeriodAsync(account.Id, YearMonth.Containing(today), cancellationToken);
            foreach (var report in await inputs.ReportsAsync(account, cancellationToken))
            {
                var due = report.SubPortfolios.Where(sub => sub.Cadence.IsDue(today) && sub.Cadence.Time <= time).Select(sub => sub.SubPortfolioId).ToHashSet();
                if (due.Count == 0)
                    continue;
                var sameCode = made.Where(each => each.Code == report.Code).ToList();
                if (sameCode.Any(each => each.Trigger == ReportTrigger.Scheduled && DateOnly.FromDateTime(ReportSchedules.Local(each.GeneratedAt).DateTime) == today))
                    continue;
                if (report.Kind == ReportKind.Monthly && sameCode.Any(each => each.IsOpen))
                    continue;

                var result = await generate.Handle(
                    new GenerateReportCommand(account.Id, report.Code, AsOf: today, SubPortfolioIds: due), ReportTrigger.Scheduled, cancellationToken);
                if (result.IsFailure)
                    logger.LogWarning("Report {Code} for account {AccountId} was not generated: {Error}", report.Code, account.Id, result.Error.Message);
            }
        }
    }
}
