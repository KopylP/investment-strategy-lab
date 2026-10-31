using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;

public sealed record ReverseTransactionCommand(string Reason, DateOnly? ReversedOn = null);

public sealed class ReverseTransactionHandler(TransactionRecorder recorder)
{
    public Task<Result<RecordedTransaction>> Handle(Guid accountId, Guid transactionId, ReverseTransactionCommand command, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context =>
            context.Journal.FirstOrDefault(entry => entry.Id.Value == transactionId) is { } original
                ? Reverse(context, original, command, TransactionSource.Manual)
                : TransactionErrors.NotFound(transactionId), cancellationToken);

    public Task<Result<RecordedTransaction>> HandleBySource(Guid accountId, string sourceReference, string reason, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context =>
            context.Journal.FirstOrDefault(entry => entry.Source.Reference == sourceReference) is { } original
                ? Reverse(context, original, new ReverseTransactionCommand(reason),
                    new TransactionSource(original.Source.Kind, $"reverse:{sourceReference}"))
                : Error.NotFound("transaction.not_found", $"No transaction was recorded from '{sourceReference}'."), cancellationToken);

    private static Result<Transaction> Reverse(RecordingContext context, Transaction original, ReverseTransactionCommand command, TransactionSource source)
    {
        if (context.Journal.Any(entry => entry.ReversalOf == original.Id))
            return TransactionErrors.AlreadyReversed(original.Id.Value);

        var reversedOn = command.ReversedOn ?? DateOnly.FromDateTime(context.Now.UtcDateTime);

        return original.Reverse(context.Account, command.Reason ?? "", reversedOn, source, context.Now);
    }
}
