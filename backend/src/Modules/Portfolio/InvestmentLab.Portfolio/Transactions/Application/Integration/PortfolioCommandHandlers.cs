using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

/// <summary>
/// Commands from other modules (fills marked in a report). A command already recorded is ignored;
/// one that cannot be recorded answers with <see cref="TransactionRejectedV1"/>.
/// </summary>
internal sealed class PortfolioCommandHandlers(
    RecordTradeHandler trades,
    RecordCashMovementHandler cash,
    RecordInternalTransferHandler transfers,
    ReverseTransactionHandler reversals,
    ITransactionJournal journal,
    TimeProvider clock)
    : IIntegrationEventHandler<RecordTradeV1>,
        IIntegrationEventHandler<RecordContributionV1>,
        IIntegrationEventHandler<RecordInternalTransferV1>,
        IIntegrationEventHandler<ReverseTransactionV1>
{
    private static readonly TransactionSource FromReport = new(TransactionSourceKind.ReportAction, null);

    public async Task Handle(RecordTradeV1 command, CancellationToken cancellationToken) =>
        await RejectOnFailure(command.SourceReference, await trades.Handle(
            command.AccountId,
            new RecordTradeCommand(command.InstrumentId, command.Side, command.Quantity, command.Price, command.Fee, command.ExecutedOn, command.Allocations),
            FromReport with { Reference = command.SourceReference },
            cancellationToken), cancellationToken);

    public async Task Handle(RecordContributionV1 command, CancellationToken cancellationToken) =>
        await RejectOnFailure(command.SourceReference, await cash.Handle(
            command.AccountId,
            new RecordContributionCommand(command.Amount, command.OccurredOn, command.Allocations),
            FromReport with { Reference = command.SourceReference },
            cancellationToken), cancellationToken);

    public async Task Handle(RecordInternalTransferV1 command, CancellationToken cancellationToken) =>
        await RejectOnFailure(command.SourceReference, await transfers.Handle(
            command.AccountId,
            new RecordInternalTransferCommand(command.FromSubPortfolioId, command.ToSubPortfolioId, command.OccurredOn, command.InstrumentId, command.Quantity, command.Amount),
            FromReport with { Reference = command.SourceReference },
            cancellationToken), cancellationToken);

    public async Task Handle(ReverseTransactionV1 command, CancellationToken cancellationToken) =>
        await RejectOnFailure(command.SourceReference,
            await reversals.HandleBySource(command.AccountId, command.SourceReference, command.Reason, cancellationToken), cancellationToken);

    private async Task RejectOnFailure(string sourceReference, Result result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess || result.Error.Code is "transaction.duplicate_source" or "transaction.already_reversed")
            return;

        journal.Publish(new TransactionRejectedV1(sourceReference, result.Error.Code, result.Error.Message), clock.GetUtcNow());
        await journal.SaveChangesAsync(cancellationToken);
    }
}
