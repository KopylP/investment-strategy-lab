using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

internal static class TransactionReversal
{
    public static Result<Transaction> Of(
        RecordingContext context, Transaction original, ReverseTransactionCommand command, TransactionSource source)
    {
        if (context.Journal.Any(entry => entry.ReversalOf == original.Id))
            return TransactionErrors.AlreadyReversed(original.Id.Value);

        var reversedOn = command.ReversedOn ?? DateOnly.FromDateTime(context.Now.UtcDateTime);

        return original.Reverse(context.Account, command.Reason ?? "", reversedOn, source, context.Now);
    }
}
