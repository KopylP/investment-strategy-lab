using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class TradeMarkedMapper : IDomainEventMapper<TradeMarked, RecordTradeV1>
{
    public RecordTradeV1 Map(TradeMarked marked)
    {
        var side = marked.IsSale ? "Sell" : "Buy";
        var allocations = marked.Allocations
            .Select(allocation => new QuantityAllocationDto(allocation.SubPortfolioId, allocation.Quantity))
            .ToList();

        return new RecordTradeV1(
            marked.AccountId,
            marked.Reference,
            marked.InstrumentId,
            side,
            marked.Quantity,
            marked.Price,
            marked.Fee,
            marked.ExecutedOn,
            allocations);
    }
}
