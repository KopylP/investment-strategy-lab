using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ReverseRecordedTransaction;

public sealed class ReverseRecordedTransactionHandler(TransactionRecorder recorder) : IHandler
{
    public Task<Result<RecordedTransaction>> Handle(
        Guid accountId, string sourceReference, string reason, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context => Reverse(context, sourceReference, reason), cancellationToken);

    private static Result<Transaction> Reverse(RecordingContext context, string sourceReference, string reason)
    {
        if (context.Journal.FirstOrDefault(entry => entry.Source.Reference == sourceReference) is not { } original)
            return Error.NotFound("transaction.not_found", $"No transaction was recorded from '{sourceReference}'.");

        var source = new TransactionSource(original.Source.Kind, $"reverse:{sourceReference}");

        return TransactionReversal.Of(context, original, new ReverseTransactionCommand(reason), source);
    }
}
