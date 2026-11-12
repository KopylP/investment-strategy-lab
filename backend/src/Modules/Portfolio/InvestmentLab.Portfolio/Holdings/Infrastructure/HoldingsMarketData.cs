using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Holdings.Domain;

namespace InvestmentLab.Portfolio.Holdings.Infrastructure;

internal sealed class HoldingsMarketData(IQuoteHistory quotes, IFxRates fxRates) : IHoldingsMarket
{
    public async Task<HoldingsMarket> OnAsync(AccountHoldings holdings, DateOnly day, CancellationToken cancellationToken)
    {
        var latest = await LatestPricesAsync(holdings, day, cancellationToken);
        var currencies = latest.Select(price => price.Currency).Concat(ForeignCashCurrencies(holdings)).Distinct();
        var rates = await RatesAsync(currencies, holdings.Currency, day, cancellationToken);

        var prices = latest
            .Where(price => rates.ContainsKey(Currency.Of(price.Currency)))
            .ToDictionary(price => price.InstrumentId, price => PriceOf(price, rates));

        return new HoldingsMarket(prices, rates);
    }

    private async Task<IReadOnlyList<LatestPriceDto>> LatestPricesAsync(AccountHoldings holdings, DateOnly day, CancellationToken cancellationToken)
    {
        var instrumentIds = holdings.SubPortfolios.SelectMany(sub => sub.Lines).Select(line => line.InstrumentId).Distinct().ToList();

        return instrumentIds.Count == 0 ? [] : await quotes.GetLatestPrices(instrumentIds, day, cancellationToken);
    }

    private static IEnumerable<string> ForeignCashCurrencies(AccountHoldings holdings) =>
        holdings.SubPortfolios.SelectMany(sub => sub.ForeignCash).Select(money => money.Currency.Code);

    private async Task<Dictionary<Currency, decimal>> RatesAsync(
        IEnumerable<string> currencies, Currency accountCurrency, DateOnly day, CancellationToken cancellationToken)
    {
        var rates = new Dictionary<Currency, decimal> { [accountCurrency] = 1m };

        foreach (var code in currencies.Where(code => code != accountCurrency.Code))
            if (await fxRates.GetCrossRate(code, accountCurrency.Code, day, cancellationToken) is { } rate)
                rates[Currency.Of(code)] = rate;

        return rates;
    }

    private static MarketPrice PriceOf(LatestPriceDto price, Dictionary<Currency, decimal> rates)
    {
        var currency = Currency.Of(price.Currency);

        return MarketPrice.Of(price.Date, price.Close, currency, rates[currency], price.IsStale);
    }
}
