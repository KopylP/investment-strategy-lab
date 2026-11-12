using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public static class ManualRecording
{
    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordTradeHandler handler, Guid accountId, RecordTradeCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSourceDto.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordContributionHandler handler, Guid accountId, RecordContributionCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSourceDto.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordCashMovementHandler handler, Guid accountId, RecordCashMovementCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSourceDto.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordInternalTransferHandler handler, Guid accountId, RecordInternalTransferCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSourceDto.Manual, cancellationToken);
}
