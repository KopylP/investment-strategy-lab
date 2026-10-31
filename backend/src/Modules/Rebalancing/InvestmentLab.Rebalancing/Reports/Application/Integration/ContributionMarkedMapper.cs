using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class ContributionMarkedMapper : IDomainEventMapper<ContributionMarked, RecordContributionV1>
{
    private const int CashDecimals = 2;

    public RecordContributionV1 Map(ContributionMarked marked)
    {
        var allocations = marked.Allocations.Select(ToAmountAllocation).ToList();

        return new RecordContributionV1(marked.AccountId, marked.Reference, marked.Amount, marked.ExecutedOn, allocations);
    }

    private static AmountAllocationDto ToAmountAllocation(ActionAllocation allocation) =>
        new(allocation.SubPortfolioId, decimal.Round(allocation.Quantity, CashDecimals));
}
