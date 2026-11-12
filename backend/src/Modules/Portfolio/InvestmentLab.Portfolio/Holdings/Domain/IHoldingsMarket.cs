namespace InvestmentLab.Portfolio.Holdings.Domain;

public interface IHoldingsMarket
{
    Task<HoldingsMarket> OnAsync(AccountHoldings holdings, DateOnly day, CancellationToken cancellationToken);
}
