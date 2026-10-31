using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class SupersedePlanOfReport(
    IPerformanceRepository store,
    SnapshotInvalidation snapshots) : IIntegrationEventHandler<ReportSupersededV1>
{
    public async Task Handle(ReportSupersededV1 superseded, CancellationToken cancellationToken)
    {
        if (await store.FindPlannedReportAsync(superseded.ReportId, cancellationToken) is not { } report)
            return;

        report.Supersede();

        await snapshots.MarkAccountAsync(report.AccountId, report.OwnerId.Value, report.MarketAsOf, cancellationToken);
    }
}
