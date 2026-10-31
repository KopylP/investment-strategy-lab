using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.ListReports;

public sealed class ListReportsHandler(IReportRepository reports)
{
    public async Task<IReadOnlyList<ReportSummaryDto>> Handle(Guid? accountId, CancellationToken cancellationToken)
    {
        var found = await reports.ListAsync(accountId, cancellationToken);

        return [.. found.Select(report => report.ToSummaryDto())];
    }
}
