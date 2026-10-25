using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

/// <summary>What a use case knows when it builds the entry: the account, its journal and current holdings.</summary>
public sealed record RecordingContext(InvestmentAccount Account, IReadOnlyList<Transaction> Journal, AccountHoldings Holdings, DateTimeOffset Now);

public sealed record RecordedTransaction(Guid TransactionId, IReadOnlyList<string> Warnings);

/// <summary>
/// Records one journal entry: checks it against the holdings replayed with it (no selling units a sub-portfolio
/// does not hold at that date), stores it with the new holdings and its integration event in one save.
/// </summary>
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
        if (transaction.ReversalOf is { } originalId)
            journal.Publish(transaction.ToReversedEvent(entries.Single(entry => entry.Id == originalId)), now);
        else
            journal.Publish(transaction.ToRecordedEvent(account.BaseCurrency), now);

        await journal.SaveChangesAsync(cancellationToken);
        return new RecordedTransaction(transaction.Id.Value, projected.Value.Warnings);
    }
}
