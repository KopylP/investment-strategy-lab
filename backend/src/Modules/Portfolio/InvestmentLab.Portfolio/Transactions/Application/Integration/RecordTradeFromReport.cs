using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class RecordTradeFromReport(RecordTradeHandler trades, CommandRejection rejection) : IIntegrationEventHandler<RecordTradeV1>
{
    public async Task Handle(RecordTradeV1 command, CancellationToken cancellationToken)
    {
        var trade = new RecordTradeCommand(
            command.InstrumentId, command.Side, command.Quantity, command.Price, command.Fee, command.ExecutedOn, command.Allocations);
        var source = TransactionSourceDto.FromReport(command.SourceReference);

        var recorded = await trades.Handle(command.AccountId, trade, source, cancellationToken);

        await rejection.RejectOnFailureAsync(command.SourceReference, recorded, cancellationToken);
    }
}
