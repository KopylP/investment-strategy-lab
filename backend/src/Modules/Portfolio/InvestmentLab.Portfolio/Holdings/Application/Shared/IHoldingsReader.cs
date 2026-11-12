using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Application.Shared;

public interface IHoldingsReader
{
    Task<InvestmentAccount?> FindAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<AccountHoldings> GetCurrentAsync(InvestmentAccount account, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetEntriesAsync(AccountId accountId, CancellationToken cancellationToken);
}
