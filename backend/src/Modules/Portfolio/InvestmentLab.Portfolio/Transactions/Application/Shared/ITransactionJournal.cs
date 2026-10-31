using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public interface ITransactionJournal
{
    Task<InvestmentAccount?> FindAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetEntriesAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<bool> SourceExistsAsync(string sourceReference, CancellationToken cancellationToken);

    void Add(Transaction transaction);

    Task ReplaceHoldingsAsync(AccountHoldings holdings, UserId ownerId, CancellationToken cancellationToken);

    void Record(IDomainEvent domainEvent);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
