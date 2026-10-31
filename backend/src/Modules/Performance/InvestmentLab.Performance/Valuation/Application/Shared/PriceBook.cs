using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed class PriceBook(IQuoteHistory quotes, IInstrumentCatalog instruments, IFxRates fxRates)
{
    public async Task<PriceOn> LoadAsync(
        IReadOnlyCollection<Guid> instrumentIds,
        string currency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var series = (await quotes.GetQuoteHistory(instrumentIds, from.AddDays(-10), to, cancellationToken))
            .ToDictionary(item => item.InstrumentId, item => item.Points.Select(point => (point.Date, point.Close)).ToList());

        var rates = new Dictionary<string, List<(DateOnly Date, decimal Rate)>>();
        async Task<List<(DateOnly Date, decimal Rate)>> PlnSeries(string code) =>
            rates.TryGetValue(code, out var cached) ? cached
            : rates[code] = [.. (await fxRates.GetFxRateHistory(code, "PLN", from.AddDays(-15), to, cancellationToken)).Select(rate => (rate.EffectiveDate, rate.Rate))];

        var factors = new Dictionary<Guid, Func<DateOnly, decimal?>>();

        foreach (var id in instrumentIds)
        {
            var trading = catalog.TryGetValue(id, out var instrument) ? instrument.TradingCurrency : currency;
            if (trading == currency)
            {
                factors[id] = _ => 1m;
                continue;
            }

            var fromPln = trading == "PLN" ? null : await PlnSeries(trading);
            var toPln = currency == "PLN" ? null : await PlnSeries(currency);
            factors[id] = day =>
            {
                var a = fromPln is null ? 1m : Last(fromPln, day);
                var b = toPln is null ? 1m : Last(toPln, day);

                return a is { } x && b is { } y && y > 0m ? x / y : null;
            };
        }

        return (instrumentId, day) =>
            series.TryGetValue(instrumentId, out var points) && Last(points, day) is { } close && factors[instrumentId](day) is { } factor
                ? close * factor
                : null;
    }

    public async Task<FxOn> LoadFxAsync(
        IReadOnlyCollection<string> currencies,
        string currency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        async Task<List<(DateOnly Date, decimal Rate)>?> PlnSeries(string code) => code == "PLN"
            ? null
            : [.. (await fxRates.GetFxRateHistory(code, "PLN", from.AddDays(-15), to, cancellationToken)).Select(rate => (rate.EffectiveDate, rate.Rate))];

        var toPln = await PlnSeries(currency);
        var series = new Dictionary<string, List<(DateOnly Date, decimal Rate)>?>();

        foreach (var code in currencies.Distinct())
            series[code] = await PlnSeries(code);

        return (code, day) =>
        {
            if (code == currency)
                return 1m;

            if (!series.TryGetValue(code, out var fromPln))
                return null;

            var a = fromPln is null ? 1m : Last(fromPln, day);
            var b = toPln is null ? 1m : Last(toPln, day);

            return a is { } x && b is { } y && y > 0m ? x / y : null;
        };
    }

    private static decimal? Last(List<(DateOnly Date, decimal Value)> points, DateOnly day)
    {
        decimal? found = null;

        foreach (var (date, value) in points)
        {
            if (date > day)
                break;

            found = value;
        }

        return found;
    }
}
