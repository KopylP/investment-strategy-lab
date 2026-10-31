using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class TransferMarkedMapper : IDomainEventMapper<TransferMarked, RecordInternalTransferV1>
{
    public RecordInternalTransferV1 Map(TransferMarked marked) => new(
        marked.AccountId,
        marked.Reference,
        marked.FromSubPortfolioId,
        marked.ToSubPortfolioId,
        marked.ExecutedOn,
        marked.InstrumentId,
        marked.Quantity,
        null);
}
