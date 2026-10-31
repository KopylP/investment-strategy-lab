using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;
using InvestmentLab.Portfolio.Transactions.Application.RecordTrade;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

public static class ManualRecording
{
    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordTradeHandler handler, Guid accountId, RecordTradeCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSource.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordContributionHandler handler, Guid accountId, RecordContributionCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSource.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordCashMovementHandler handler, Guid accountId, RecordCashMovementCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSource.Manual, cancellationToken);

    public static Task<Result<RecordedTransaction>> RecordManually(
        this RecordInternalTransferHandler handler, Guid accountId, RecordInternalTransferCommand command, CancellationToken cancellationToken) =>
        handler.Handle(accountId, command, TransactionSource.Manual, cancellationToken);
}
