using System.Globalization;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GetReportSchedule;

public sealed class GetReportScheduleHandler(PlanningInputs inputs, IPlanSettingsRepository settings, TimeProvider clock)
{
    private const string LocalMinuteFormat = "yyyy-MM-ddTHH:mm";

    public async Task<Result<ReportScheduleDto>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken);
        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;

        var reports = await inputs.ReportsAsync(account, current, cancellationToken);
        var scheduled = reports.SelectMany(report => ScheduledEntries(report, now)).ToList();

        return new ReportScheduleDto(current?.GenerateOnSchedule ?? true, ReportSchedules.TimeZone, scheduled);
    }

    private static IEnumerable<ScheduledReportDto> ScheduledEntries(AccountReport report, DateTime now) =>
        report.SubPortfolios
            .GroupBy(report.CadenceOf)
            .Select(sameCadence => ToDto(report, sameCadence, now));

    private static ScheduledReportDto ToDto(AccountReport report, IGrouping<Cadence, SubPortfolioReport> sameCadence, DateTime now)
    {
        var next = sameCadence.Key.Next(now)?.ToString(LocalMinuteFormat, CultureInfo.InvariantCulture);
        var strategies = sameCadence.Select(sub => $"{sub.SubPortfolioName}: {sub.StrategyName}").ToList();

        return new ScheduledReportDto(
            report.Code,
            report.Name,
            report.Description,
            report.Mode,
            ReportTimingDto.Of(sameCadence.Key),
            ReportTimingDto.Of(sameCadence.First().Default),
            report.Own is not null,
            next,
            strategies);
    }
}
