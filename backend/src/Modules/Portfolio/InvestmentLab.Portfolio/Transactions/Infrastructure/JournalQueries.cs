using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class JournalQueries(ITransactionJournal journal) : IJournalQueries
{
    public async Task<JournalSnapshotDto?> GetJournal(Guid accountId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return null;

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);

        return new JournalSnapshotDto(
            account.Id.Value,
            account.OwnerId.Value,
            account.BaseCurrency.Code,
            [.. entries.OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.RecordedAt).Select(entry => entry.ToRecordedEvent(account.BaseCurrency))],
            [.. entries.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value.Value)]);
    }
}
