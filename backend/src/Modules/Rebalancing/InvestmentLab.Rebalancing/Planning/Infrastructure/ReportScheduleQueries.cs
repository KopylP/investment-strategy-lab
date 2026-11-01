using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class ReportScheduleQueries(IAccountSetupQueries accounts, IAccountReports accountReports, IPlanSettingsRepository settings, TimeProvider clock) : IReportScheduleQueries
{
    public async Task<DateTime?> NextAsync(Guid accountId, string code, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return null;

        var current = await settings.FindAsync(accountId, cancellationToken);
        if (current is { GenerateOnSchedule: false })
            return null;

        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;
        var reports = await accountReports.OfAsync(account.Id, current, cancellationToken);

        return reports
            .Where(report => report.Code == code)
            .SelectMany(report => report.SubPortfolios.Select(sub => report.CadenceOf(sub).Next(now)))
            .Min();
    }
}
