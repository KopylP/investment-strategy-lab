using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Infrastructure;

internal sealed class ReportQueries(IReportRepository reports) : IReportQueries
{
    public async Task<int> GetOpenActionsCount(Guid accountId, CancellationToken cancellationToken) =>
        (await reports.OpenAsync(accountId, cancellationToken)).Sum(report => report.Actions.Count(action => !action.IsResolved));
}
