using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class KeepPlanOfGeneratedReport(IPerformanceRepository store, SnapshotInvalidation snapshots) : IIntegrationEventHandler<ReportGeneratedV1>
{
    public async Task Handle(ReportGeneratedV1 generated, CancellationToken cancellationToken)
    {
        if (await store.FindPlannedReportAsync(generated.ReportId, cancellationToken) is null)
            store.AddPlannedReport(ToPlannedReport(generated));

        await snapshots.MarkAccountAsync(generated.AccountId, generated.OwnerId, generated.MarketAsOf, cancellationToken);
    }

    private static PlannedReport ToPlannedReport(ReportGeneratedV1 generated)
    {
        var actions = generated.Actions.Select(ToPlannedAction).ToList();

        return PlannedReport.Of(
            generated.ReportId,
            generated.AccountId,
            UserId.Of(generated.OwnerId),
            generated.Kind,
            generated.Period,
            generated.MarketAsOf,
            generated.GeneratedAt,
            actions);
    }

    private static PlannedAction ToPlannedAction(PlannedActionDto action)
    {
        var parts = action.Allocations.Select(part => new PlannedPart(part.SubPortfolioId, part.Quantity)).ToList();

        return new PlannedAction(action.Kind, action.InstrumentId, action.Quantity, action.Price, parts, action.FromSubPortfolioId);
    }
}
