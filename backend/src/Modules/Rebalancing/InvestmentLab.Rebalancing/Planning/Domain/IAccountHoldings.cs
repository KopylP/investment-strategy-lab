namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IAccountHoldings
{
    Task<IReadOnlyList<IHeldSubPortfolio>> OfAsync(Guid accountId, string accountCurrency, CancellationToken cancellationToken);
}
