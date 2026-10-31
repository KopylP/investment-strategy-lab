namespace InvestmentLab.Portfolio.Contracts.Accounts;

public interface IAccountSetupQueries
{
    Task<AccountSetupDto?> GetAccountSetup(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountSetupDto>> GetAccountSetups(CancellationToken cancellationToken);
}
