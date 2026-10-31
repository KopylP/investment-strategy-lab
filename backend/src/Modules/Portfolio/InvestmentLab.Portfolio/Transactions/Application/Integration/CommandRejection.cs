using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class CommandRejection(ITransactionJournal journal, TimeProvider clock)
{
    public static readonly TransactionSource FromReport = new(TransactionSourceKind.ReportAction, null);

    public async Task RejectOnFailureAsync(string sourceReference, Result result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess || IsAlreadyDone(result.Error))
            return;

        journal.Record(new TransactionRejected(sourceReference, result.Error.Code, result.Error.Message, clock.GetUtcNow()));

        await journal.SaveChangesAsync(cancellationToken);
    }

    private static bool IsAlreadyDone(Error error) => error.Code is "transaction.duplicate_source" or "transaction.already_reversed";
}
