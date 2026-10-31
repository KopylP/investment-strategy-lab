using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed class TransactionRecorder(ITransactionJournal journal, TimeProvider clock)
{
    public async Task<Result<RecordedTransaction>> RecordAsync(
        Guid accountId, Func<RecordingContext, Result<Transaction>> create, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);
        var current = HoldingsProjector.Project(account, entries);
        if (current.IsFailure)
            return current.Error;

        var now = clock.GetUtcNow();
        var created = create(new RecordingContext(account, entries, current.Value.Holdings, now));
        if (created.IsFailure)
            return created.Error;

        var transaction = created.Value;
        if (transaction.Source.Reference is { } reference && await journal.SourceExistsAsync(reference, cancellationToken))
            return TransactionErrors.DuplicateSource(reference);

        var projected = HoldingsProjector.Project(account, [.. entries, transaction]);
        if (projected.IsFailure)
            return projected.Error;

        journal.Add(transaction);
        await journal.ReplaceHoldingsAsync(projected.Value.Holdings, account.OwnerId, cancellationToken);
        Announce(transaction, account, entries, now);

        await journal.SaveChangesAsync(cancellationToken);

        return new RecordedTransaction(transaction.Id.Value, projected.Value.Warnings);
    }

    private static void Announce(Transaction transaction, InvestmentAccount account, IReadOnlyList<Transaction> entries, DateTimeOffset now)
    {
        if (transaction.ReversalOf is not { } originalId)
        {
            transaction.Recorded(account.BaseCurrency, now);
            return;
        }

        var original = entries.Single(entry => entry.Id == originalId);

        transaction.Reversed(original, now);
    }
}
