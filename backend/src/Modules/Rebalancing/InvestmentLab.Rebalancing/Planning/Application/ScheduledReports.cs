using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application;

public static class ReportSchedules
{
    public const string TimeZone = "Europe/Warsaw";

    /// <summary>Sunday evening, after all markets closed on Friday: the plan is executed on Monday (as the Python flow).</summary>
    public static readonly DailySchedule Monthly = new(new TimeOnly(20, 0), TimeZoneInfo.FindSystemTimeZoneById(TimeZone), WeekdaysOnly: false);

    public static readonly DailySchedule Weekly = new(new TimeOnly(19, 0), TimeZoneInfo.FindSystemTimeZoneById(TimeZone), WeekdaysOnly: false);

    public static DateOnly Today(TimeProvider clock) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(TimeZone)).DateTime);

}

/// <summary>
/// Runs daily; generates the rebalancing report for every account whose strategies want one today (weekly, the first
/// Sunday of the month or of the quarter — their "reporting" parameters), except accounts that already have an open
/// report for the month (D14: a user's report is never overwritten automatically).
/// </summary>
public sealed class MonthlyReportJob(
    PlanningInputs inputs, IPlanSettingsRepository settings, IReportRepository reports, GenerateMonthlyReportHandler generate,
    TimeProvider clock, ILogger<MonthlyReportJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = ReportSchedules.Today(clock);
        if (today.DayOfWeek != DayOfWeek.Sunday)
            return;

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
        {
            if (await settings.FindAsync(account.Id, cancellationToken) is { GenerateOnSchedule: false })
                continue;
            if (!(await inputs.CadencesAsync(account, cancellationToken)).Any(each => each.Cadence.IsRebalanceDay(today)))
                continue;
            var period = YearMonth.Containing(today);
            if ((await reports.ForPeriodAsync(account.Id, period, cancellationToken)).Any(report => report.IsOpen && report.Kind != ReportKind.RiskReview))
                continue;

            var result = await generate.Handle(new GenerateMonthlyReportCommand(account.Id, AsOf: today), ReportTrigger.Scheduled, cancellationToken);
            if (result.IsFailure)
                logger.LogWarning("Monthly report for account {AccountId} was not generated: {Error}", account.Id, result.Error.Message);
        }
    }
}

/// <summary>Every Sunday: the risk review of every account with a strategy that asks for it.</summary>
public sealed class WeeklyRiskReviewJob(
    PlanningInputs inputs, IPlanSettingsRepository settings, GenerateRiskReviewHandler generate, TimeProvider clock, ILogger<WeeklyRiskReviewJob> logger)
    : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = ReportSchedules.Today(clock);
        if (today.DayOfWeek != DayOfWeek.Sunday)
            return;

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
        {
            if (await settings.FindAsync(account.Id, cancellationToken) is { GenerateOnSchedule: false })
                continue;
            if (!(await inputs.CadencesAsync(account, cancellationToken)).Any(each => each.Cadence.IsRiskReviewDay(today)))
                continue;

            var result = await generate.Handle(new GenerateRiskReviewCommand(account.Id, today), ReportTrigger.Scheduled, cancellationToken);
            if (result.IsFailure)
                logger.LogWarning("Risk review for account {AccountId} was not generated: {Error}", account.Id, result.Error.Message);
        }
    }
}
