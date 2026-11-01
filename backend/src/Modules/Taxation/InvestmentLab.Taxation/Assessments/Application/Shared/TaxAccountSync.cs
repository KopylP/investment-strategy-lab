using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxAccountSync(ITaxLedgerRepository ledger, ITaxpayerAccounts accounts, ITaxJournals journals)
{
    public async Task<TaxAccount?> EnsureAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.FindAsync(accountId, cancellationToken);
        if (setup is null || await journals.FindAsync(accountId, cancellationToken) is not { } journal)
            return null;

        var account = await SynchronizedAccountAsync(setup, cancellationToken);

        await CopyJournalAsync(accountId, journal, cancellationToken);
        await ledger.SaveChangesAsync(cancellationToken);

        return account;
    }

    private async Task<TaxAccount> SynchronizedAccountAsync(ITaxpayerAccount setup, CancellationToken cancellationToken)
    {
        var account = await ledger.FindAccountAsync(setup.Id, cancellationToken);

        if (account is not null)
        {
            account.Update(setup.Currency, setup.PolicyCode);

            return account;
        }

        var added = TaxAccount.Of(setup.Id, setup.OwnerId, setup.Currency, setup.PolicyCode);
        ledger.AddAccount(added);

        return added;
    }

    private async Task CopyJournalAsync(Guid accountId, TaxJournal journal, CancellationToken cancellationToken)
    {
        var copied = (await ledger.EntriesAsync(accountId, cancellationToken)).ToDictionary(entry => entry.TransactionId);
        var reversed = journal.ReversedIds.ToHashSet();
        var recordedAt = DateTimeOffset.UnixEpoch;

        foreach (var recorded in journal.Entries)
        {
            recordedAt = recordedAt.AddSeconds(1);

            var entry = CopyEntry(recorded, copied.GetValueOrDefault(recorded.TransactionId), recordedAt);

            if (entry is { IsReversed: false } && reversed.Contains(entry.TransactionId))
                entry.Reverse();
        }
    }

    private TaxLedgerEntry? CopyEntry(ITaxJournalEntry recorded, TaxLedgerEntry? existing, DateTimeOffset recordedAt)
    {
        if (existing is null)
        {
            var added = LedgerEntries.From(recorded, recordedAt);

            if (added is not null)
                ledger.AddEntry(added);

            return added;
        }

        if (LedgerEntries.From(recorded, existing.RecordedAt) is { } current)
            existing.Restate(current);

        return existing;
    }
}
