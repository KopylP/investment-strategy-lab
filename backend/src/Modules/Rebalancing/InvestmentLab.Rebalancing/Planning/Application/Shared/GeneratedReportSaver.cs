using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed class GeneratedReportSaver(IReportRepository reports, SaleTaxes taxes)
{
    public async Task SaveAsync(
        RebalancingReport report,
        IEnumerable<RebalancingReport> replaceable,
        DateOnly asOf,
        DateTimeOffset now,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        await taxes.EstimateAsync(report, asOf, cancellationToken);
        reports.Add(report);

        report.SupersedeOpen(replaceable, now);
        report.Generated(request.RequestedAt, now);
        request.Complete(report.Id, now);

        await reports.SaveChangesAsync(cancellationToken);
    }
}
