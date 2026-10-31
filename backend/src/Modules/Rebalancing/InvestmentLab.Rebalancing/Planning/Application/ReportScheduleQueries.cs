using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

internal sealed class ReportScheduleQueries(PlanningInputs inputs, IPlanSettingsRepository settings, TimeProvider clock) : IReportScheduleQueries
{
    public async Task<DateTime?> NextAsync(Guid accountId, string code, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return null;

        var current = await settings.FindAsync(accountId, cancellationToken);
        if (current is { GenerateOnSchedule: false })
            return null;

        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;
        var reports = await inputs.ReportsAsync(account, current, cancellationToken);

        return reports
            .Where(report => report.Code == code)
            .SelectMany(report => report.SubPortfolios.Select(sub => report.CadenceOf(sub).Next(now)))
            .Min();
    }
}
