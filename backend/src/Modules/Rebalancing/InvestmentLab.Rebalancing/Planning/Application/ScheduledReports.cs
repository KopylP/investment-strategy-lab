using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Rebalancing.Planning.Application.GenerateReport;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application;

public static class ReportSchedules
{
    public const string TimeZone = "Europe/Warsaw";

    /// <summary>The job looks this often whether a report's time has come: an hourly report set to :05 comes at :05.</summary>
    public static readonly IntervalSchedule Check = new(TimeSpan.FromMinutes(1));

    /// <summary>A report time missed for longer (the service was down) is skipped: the next one comes as scheduled.</summary>
    public static readonly TimeSpan CatchUp = TimeSpan.FromDays(1);

    public static DateTimeOffset Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(TimeZone));
}

/// <summary>
/// Makes every report the accounts' strategies declare once its time has come (the user's own for the account, else
/// the strategy's): for the sub-portfolios whose latest report time (within <see cref="ReportSchedules.CatchUp"/>) is
/// later than the last report of that code made on schedule and than the moment the user set their own time. A
/// rebalance report is not made while one of the same code is still open for the month (D14: a user's report is never
/// overwritten automatically).
/// </summary>
public sealed class StrategyReportsJob(
    PlanningInputs inputs, IPlanSettingsRepository settings, IReportRepository reports, GenerateReportHandler generate,
    TimeProvider clock, ILogger<StrategyReportsJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;
        var today = DateOnly.FromDateTime(now);

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
        {
            var plan = await settings.FindAsync(account.Id, cancellationToken);
            if (plan is { GenerateOnSchedule: false })
                continue;

            IReadOnlyList<RebalancingReport>? made = null;
            foreach (var report in await inputs.ReportsAsync(account, plan, cancellationToken))
            {
                var times = report.SubPortfolios
                    .Select(sub => (sub.SubPortfolioId, Time: report.CadenceOf(sub).Last(now)))
                    .Where(each => each.Time is { } time && now - time < ReportSchedules.CatchUp)
                    .ToList();
                if (times.Count == 0)
                    continue;

                // A time before the user set their own does not count; neither does one a report was already made for.
                var since = report.Own is { } own ? ReportSchedules.Local(own.Since).DateTime : DateTime.MinValue;
                var last = await reports.LastScheduledAsync(account.Id, report.Code, cancellationToken) is { } at
                    ? ReportSchedules.Local(at).DateTime
                    : DateTime.MinValue;
                if (since > last)
                    last = since;
                var due = times.Where(each => each.Time > last).Select(each => each.SubPortfolioId).ToHashSet();
                if (due.Count == 0)
                    continue;
                made ??= await reports.ForPeriodAsync(account.Id, YearMonth.Containing(today), cancellationToken);
                if (report.Kind == ReportKind.Monthly && made.Any(each => each.Code == report.Code && each.IsOpen))
                    continue;

                var result = await generate.Handle(
                    new GenerateReportCommand(account.Id, report.Code, AsOf: today, SubPortfolioIds: due), ReportTrigger.Scheduled, cancellationToken);
                if (result.IsFailure)
                    logger.LogWarning("Report {Code} for account {AccountId} was not generated: {Error}", report.Code, account.Id, result.Error.Message);
            }
        }
    }
}
