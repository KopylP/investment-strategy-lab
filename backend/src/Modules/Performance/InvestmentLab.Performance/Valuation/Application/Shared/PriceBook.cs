using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed class PriceBook(IQuoteHistory quotes, IInstrumentCatalog instruments, IFxRates fxRates)
{
    private const int QuoteLookbackDays = 10;
    private const int RateLookbackDays = 15;
    private const string Pln = "PLN";

    public async Task<PriceOn> LoadAsync(
        IReadOnlyCollection<Guid> instrumentIds,
        string currency,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var closes = await ClosesAsync(instrumentIds, from, to, cancellationToken);
        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var rates = new RateSeries(fxRates, from, to);
        var factors = new Dictionary<Guid, Func<DateOnly, decimal?>>();

        foreach (var id in instrumentIds)
        {
            var trading = catalog.TryGetValue(id, out var instrument) ? instrument.TradingCurrency : currency;
            factors[id] = await rates.FactorAsync(trading, currency, cancellationToken);
        }

        return (instrumentId, day) =>
            closes.TryGetValue(instrumentId, out var points) && Last(points, day) is { } close && factors[instrumentId](day) is { } factor
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
        var rates = new RateSeries(fxRates, from, to);
        var factors = new Dictionary<string, Func<DateOnly, decimal?>>();

        foreach (var code in currencies.Distinct())
            factors[code] = await rates.FactorAsync(code, currency, cancellationToken);

        return (code, day) => factors.TryGetValue(code, out var factor) ? factor(day) : code == currency ? 1m : null;
    }

    private async Task<Dictionary<Guid, List<(DateOnly Date, decimal Value)>>> ClosesAsync(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var history = await quotes.GetQuoteHistory(instrumentIds, from.AddDays(-QuoteLookbackDays), to, cancellationToken);

        return history.ToDictionary(item => item.InstrumentId, item => item.Points.Select(point => (point.Date, point.Close)).ToList());
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

    private sealed class RateSeries(IFxRates fxRates, DateOnly from, DateOnly to)
    {
        private readonly Dictionary<string, List<(DateOnly Date, decimal Value)>> _inPln = [];

        public async Task<Func<DateOnly, decimal?>> FactorAsync(string source, string target, CancellationToken cancellationToken)
        {
            if (source == target)
                return _ => 1m;

            var sourceInPln = await InPlnAsync(source, cancellationToken);
            var targetInPln = await InPlnAsync(target, cancellationToken);

            return day => FactorOn(sourceInPln, targetInPln, day);
        }

        private static decimal? FactorOn(List<(DateOnly Date, decimal Value)>? source, List<(DateOnly Date, decimal Value)>? target, DateOnly day)
        {
            var sourceRate = source is null ? 1m : Last(source, day);
            var targetRate = target is null ? 1m : Last(target, day);

            return sourceRate is { } numerator && targetRate is { } denominator && denominator > 0m ? numerator / denominator : null;
        }

        private async Task<List<(DateOnly Date, decimal Value)>?> InPlnAsync(string currency, CancellationToken cancellationToken)
        {
            if (currency == Pln)
                return null;

            if (_inPln.TryGetValue(currency, out var cached))
                return cached;

            var history = await fxRates.GetFxRateHistory(currency, Pln, from.AddDays(-RateLookbackDays), to, cancellationToken);

            return _inPln[currency] = [.. history.Select(rate => (rate.EffectiveDate, rate.Rate))];
        }
    }
}
