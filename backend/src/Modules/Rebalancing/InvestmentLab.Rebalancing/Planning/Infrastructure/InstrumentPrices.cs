using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class InstrumentPrices(IQuoteHistory quotes, IFxRates fxRates) : IInstrumentPrices
{
    public async Task<PricesInCurrency> InCurrencyAsync(IEnumerable<Guid> instrumentIds, DateOnly asOf, string currency, CancellationToken cancellationToken)
    {
        var ids = instrumentIds.Distinct().ToList();
        if (ids.Count == 0)
            return PriceConversion.Of([], new Dictionary<string, decimal?>(), currency);

        var latest = await quotes.GetLatestPrices(ids, asOf, cancellationToken);
        List<IQuotedPrice> quoted = [.. latest.Select(price => new QuotedPrice(price))];

        var rates = new Dictionary<string, decimal?>();

        foreach (var foreign in PriceConversion.ForeignCurrencies(quoted, currency))
            rates[foreign] = await fxRates.GetCrossRate(foreign, currency, asOf, cancellationToken);

        return PriceConversion.Of(quoted, rates, currency);
    }

    private sealed class QuotedPrice(LatestPriceDto dto) : IQuotedPrice
    {
        private readonly LatestPriceDto _dto = dto;

        public Guid InstrumentId => _dto.InstrumentId;
        public decimal Close => _dto.Close;
        public DateOnly Date => _dto.Date;
        public string Currency => _dto.Currency;
        public bool IsStale => _dto.IsStale;
    }
}
