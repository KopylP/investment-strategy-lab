using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class ReportGeneratedMapper : IDomainEventMapper<ReportGenerated, ReportGeneratedV1>
{
    public ReportGeneratedV1 Map(ReportGenerated generated)
    {
        var report = generated.Report;
        var actions = report.Actions.Select(ToPlannedAction).ToList();

        return new ReportGeneratedV1(
            ReportId: report.Id.Value,
            AccountId: report.AccountId,
            OwnerId: report.OwnerId.Value,
            Period: report.Period.ToString(),
            Kind: report.Kind.ToString(),
            MarketAsOf: report.MarketAsOf,
            Currency: report.Currency.Code,
            GeneratedAt: report.GeneratedAt,
            Actions: actions,
            AccountName: report.AccountName,
            Trigger: report.Trigger.ToString(),
            Title: report.Title,
            RequestedAt: generated.RequestedAt,
            Code: report.Code);
    }

    private static PlannedActionDto ToPlannedAction(ReportAction action)
    {
        var allocations = action.Allocations
            .Select(allocation => new PlannedAllocationDto(allocation.SubPortfolioId, allocation.Quantity))
            .ToList();

        return new PlannedActionDto(
            action.Id.Value,
            action.Kind.ToString(),
            action.InstrumentId,
            action.PlannedQuantity,
            action.PlannedPrice,
            action.PlannedAmount,
            allocations,
            action.FromSubPortfolioId);
    }
}
