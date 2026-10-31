using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.RecordInternalTransfer;

public sealed record RecordInternalTransferCommand(
    Guid FromSubPortfolioId,
    Guid ToSubPortfolioId,
    DateOnly OccurredOn,
    Guid? InstrumentId = null,
    decimal? Quantity = null,
    decimal? Amount = null);

public sealed class RecordInternalTransferHandler(TransactionRecorder recorder)
{
    public Task<Result<RecordedTransaction>> Handle(
        Guid accountId, RecordInternalTransferCommand command, TransactionSource source, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context => Create(context, command, source), cancellationToken);

    private static Result<Transaction> Create(RecordingContext context, RecordInternalTransferCommand command, TransactionSource source)
    {
        if (command.FromSubPortfolioId == Guid.Empty || command.ToSubPortfolioId == Guid.Empty)
            return TransactionErrors.UnknownSubPortfolio(Guid.Empty);

        if (command.Quantity is < 0m || command.Amount is < 0m)
            return TransactionErrors.AmountNotPositive();

        return Transaction.InternalTransfer(
            context.Account,
            SubPortfolioId.Of(command.FromSubPortfolioId),
            SubPortfolioId.Of(command.ToSubPortfolioId),
            command.InstrumentId,
            command.Quantity is { } quantity ? Quantity.Of(quantity) : null,
            command.Amount is { } amount ? Money.Of(amount, context.Account.BaseCurrency) : null,
            command.OccurredOn,
            source,
            context.Now);
    }
}
