using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;

public sealed record ReverseTransactionCommand(string Reason, DateOnly? ReversedOn = null);

public sealed class ReverseTransactionHandler(TransactionRecorder recorder)
{
    public Task<Result<RecordedTransaction>> Handle(
        Guid accountId, Guid transactionId, ReverseTransactionCommand command, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context => Reverse(context, transactionId, command), cancellationToken);

    private static Result<Transaction> Reverse(RecordingContext context, Guid transactionId, ReverseTransactionCommand command)
    {
        if (context.Journal.FirstOrDefault(entry => entry.Id.Value == transactionId) is not { } original)
            return TransactionErrors.NotFound(transactionId);

        return TransactionReversal.Of(context, original, command, TransactionSource.Manual);
    }
}
