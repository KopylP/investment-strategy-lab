using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class PriceTables(
    IQuoteHistory quotes,
    IInstrumentCatalog instruments,
    IFxRates fxRates,
    IFxRateBackfill fxBackfill,
    TimeProvider clock) : IPriceTables
{
    private const int YearsOfHistory = 25;

    public async Task<PriceTable> LoadAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var from = today.AddYears(-YearsOfHistory);

        var history = await quotes.GetQuoteHistoryWithArchive(setup.PricedInstruments, from, today, cancellationToken);
        var foreign = await ForeignCurrenciesAsync(setup, cancellationToken);
        var rates = await RatesAsync([.. foreign.Values.Distinct()], setup.Currency, from, today, cancellationToken);

        var byInstrument = history.ToDictionary(
            series => series.InstrumentId,
            series => InReplayCurrency(series, foreign, rates));

        return new PriceTable(byInstrument);
    }

    private async Task<Dictionary<Guid, string>> ForeignCurrenciesAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var catalog = await instruments.GetInstruments(cancellationToken);
        var priced = setup.PricedInstruments.ToHashSet();
        var moneyPriced = catalog.Where(instrument => priced.Contains(instrument.Id) && !instrument.IsVolatilityIndex);
        var foreign = moneyPriced.Where(instrument => !string.Equals(instrument.TradingCurrency, setup.Currency, StringComparison.OrdinalIgnoreCase));

        return foreign.ToDictionary(instrument => instrument.Id, instrument => instrument.TradingCurrency.ToUpperInvariant());
    }

    private async Task<Dictionary<string, IReadOnlyList<DatedRate>>> RatesAsync(
        IReadOnlyList<string> currencies, string replayCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var rates = new Dictionary<string, IReadOnlyList<DatedRate>>();
        if (currencies.Count == 0)
            return rates;

        if (!await fxBackfill.EnsureSinceAsync(from, cancellationToken))
            throw new InvalidOperationException($"Exchange rates since {from} could not be loaded for a replay in {replayCurrency}.");

        foreach (var currency in currencies)
        {
            var history = await fxRates.GetCrossRateHistory(currency, replayCurrency, from, to, cancellationToken);
            rates[currency] = [.. history.Select(rate => new DatedRate(rate.Date, rate.Rate))];
        }

        return rates;
    }

    private static IReadOnlyList<PricedDay> InReplayCurrency(
        QuoteSeriesDto series, Dictionary<Guid, string> foreign, Dictionary<string, IReadOnlyList<DatedRate>> rates)
    {
        IReadOnlyList<PricedDay> prices = [.. series.Points.Select(point => new PricedDay(point.Date, point.AdjustedClose))];

        return foreign.TryGetValue(series.InstrumentId, out var currency)
            ? CurrencyConversion.Converted(prices, rates[currency])
            : prices;
    }
}
