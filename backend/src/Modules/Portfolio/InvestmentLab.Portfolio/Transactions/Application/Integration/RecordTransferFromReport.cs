using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class RecordTransferFromReport(
    RecordInternalTransferHandler transfers,
    CommandRejection rejection) : IIntegrationEventHandler<RecordInternalTransferV1>
{
    public async Task Handle(RecordInternalTransferV1 command, CancellationToken cancellationToken)
    {
        var transfer = new RecordInternalTransferCommand(
            command.FromSubPortfolioId, command.ToSubPortfolioId, command.OccurredOn, command.InstrumentId, command.Quantity, command.Amount);
        var source = TransactionSourceDto.FromReport(command.SourceReference);

        var recorded = await transfers.Handle(command.AccountId, transfer, source, cancellationToken);

        await rejection.RejectOnFailureAsync(command.SourceReference, recorded, cancellationToken);
    }
}
