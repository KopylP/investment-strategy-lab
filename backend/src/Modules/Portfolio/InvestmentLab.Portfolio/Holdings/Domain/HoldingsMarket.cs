namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record HoldingsMarket(
    IReadOnlyDictionary<Guid, MarketPrice> Prices,
    IReadOnlyDictionary<Currency, decimal> RatesToAccountCurrency)
{
    public MarketPrice? PriceOf(Guid instrumentId) => Prices.GetValueOrDefault(instrumentId);

    public decimal InAccountCurrency(Money money) => money.Amount * RatesToAccountCurrency.GetValueOrDefault(money.Currency);
}
