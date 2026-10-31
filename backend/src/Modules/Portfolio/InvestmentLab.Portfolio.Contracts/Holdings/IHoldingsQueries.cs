namespace InvestmentLab.Portfolio.Contracts.Holdings;

public interface IHoldingsQueries
{
    Task<AccountHoldingsDto?> GetHoldings(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken);

    Task<SubPortfolioHoldingsDto?> GetSubPortfolioSnapshot(Guid accountId, Guid subPortfolioId, DateOnly? asOf, CancellationToken cancellationToken);
}
