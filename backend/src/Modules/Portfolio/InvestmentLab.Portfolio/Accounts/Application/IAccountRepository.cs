using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Accounts.Application;

public interface IAccountRepository
{
    Task<InvestmentAccount?> FindAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InvestmentAccount>> ListAsync(CancellationToken cancellationToken);

    void Add(InvestmentAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
