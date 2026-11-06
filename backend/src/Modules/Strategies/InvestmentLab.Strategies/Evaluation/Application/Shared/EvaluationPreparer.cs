using InvestmentLab.Signals.Contracts;
using InvestmentLab.Strategies.Evaluation.Domain;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.MarketData.Contracts.FxRates;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed class EvaluationPreparer(IStrategyReader strategies, IQuoteHistory quotes, IInstrumentCatalog instruments, IMarketSignalStates marketSignals,
    IParameterChecks checks, IFxRates fxRates, IFxRateBackfill fxBackfill)
{
    private const int HistoryDays = 2 * 365 + 14;

    public async Task<Result<PreparedEvaluation>> PrepareAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        if (request.StrategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(request.StrategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(request.StrategyId);

        if (strategy.Version(request.Version) is not { } version)
            return StrategyErrors.VersionNotFound(request.StrategyId, request.Version!.Value);

        var signals = await SignalStatesAsync(version.SignalIds, request, cancellationToken);
        if (signals.IsFailure)
            return signals.Error;

        var cashInstrumentId = request.CashInstrumentId ?? version.CashInstrumentId;
        var instrumentIds = InstrumentsToPrice(request, version, cashInstrumentId);
        var check = await checks.CheckAsync(strategy.TypeCode, version.Parameters, cancellationToken);
        if (check.IsFailure)
            return check.Error;
        if (!check.Value.IsClean)
            return StrategyErrors.ParametersRefused(check.Value.Problems);
        // Observations are trading sessions; leave ample room for weekends and market holidays.
        var historyDays = Math.Max(HistoryDays, checked(check.Value.HistoryBars * 2 + 30));
        var series = await MarketSeriesAsync(instrumentIds, version, request.AsOf, historyDays, cancellationToken);
        if (request.ValuationCurrency is { } currency)
        {
            if (!string.Equals(currency, request.SubPortfolio.Currency, StringComparison.OrdinalIgnoreCase))
                return EvaluationErrors.EngineRejected("Valuation prices and holding costs must use the same portfolio currency.");
            var valued = await WithValuationsAsync(series, request.SubPortfolio.Holdings.Select(holding => holding.InstrumentId).ToHashSet(), currency, request.AsOf, cancellationToken);
            if (valued.IsFailure)
                return valued.Error;
            series = valued.Value;
        }
        var universe = await UniverseAsync(version, cancellationToken);

        var context = new StrategyEvaluationContext(
            request.AsOf,
            version.Parameters,
            universe,
            cashInstrumentId,
            series,
            request.SubPortfolio,
            request.Contribution,
            signals.Value);

        return new PreparedEvaluation(strategy, version, context);
    }

    private async Task<Result<IReadOnlyList<SignalState>>> SignalStatesAsync(
        IReadOnlyList<Guid> referenced, EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        if (referenced.Count == 0)
            return Array.Empty<SignalState>();

        if (request.SignalStates is { } replayed)
        {
            var known = replayed.Select(state => new SignalState(state.SignalId, state.ActiveFor)).ToList();

            return SignalStates.For(referenced, known);
        }

        var current = await marketSignals.GetStatesAsync(referenced, cancellationToken);
        var live = current.Select(state => new SignalState(state.SignalId, state.ActiveFor)).ToList();

        return SignalStates.For(referenced, live);
    }

    private static List<Guid> InstrumentsToPrice(EvaluateStrategyRequest request, StrategyVersion version, Guid? cashInstrumentId)
    {
        var held = request.SubPortfolio.Holdings.Select(holding => holding.InstrumentId);
        IEnumerable<Guid> cash = cashInstrumentId is { } id ? [id] : [];

        return [.. version.Universe.Concat(held).Concat(cash).Distinct()];
    }

    private async Task<List<MarketSeries>> MarketSeriesAsync(
        List<Guid> instrumentIds, StrategyVersion version, DateOnly asOf, int historyDays, CancellationToken cancellationToken)
    {
        var history = await HistoryAsync(instrumentIds, version, asOf, historyDays, cancellationToken);
        IReadOnlyList<MarketPoint> PointsOf(Guid id) => history.TryGetValue(id, out var points) ? points : [];

        var series = instrumentIds.Select(id => new MarketSeries(id, SeriesRole.Instrument, PointsOf(id))).ToList();
        series.Add(new MarketSeries(version.BenchmarkInstrumentId, SeriesRole.Benchmark, PointsOf(version.BenchmarkInstrumentId)));

        if (version.VolatilityIndexInstrumentId is { } volatility)
            series.Add(new MarketSeries(volatility, SeriesRole.VolatilityIndex, PointsOf(volatility)));

        var extraNotPricedAsInstruments = version.ExtraSeries.Where(id => !instrumentIds.Contains(id));
        series.AddRange(extraNotPricedAsInstruments.Select(id => new MarketSeries(id, SeriesRole.Extra, PointsOf(id))));

        return series;
    }

    private async Task<Dictionary<Guid, IReadOnlyList<MarketPoint>>> HistoryAsync(
        List<Guid> instrumentIds, StrategyVersion version, DateOnly asOf, int historyDays, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> volatilityIndex = version.VolatilityIndexInstrumentId is { } id ? [id] : [];
        var marketIds = volatilityIndex.Prepend(version.BenchmarkInstrumentId).Concat(version.ExtraSeries);
        Guid[] requestedIds = [.. instrumentIds.Concat(marketIds).Distinct()];

        var history = await quotes.GetQuoteHistoryWithArchive(requestedIds, asOf.AddDays(-historyDays), asOf, cancellationToken);

        return history.ToDictionary(series => series.InstrumentId, series => MarketPointsOf(series.Points));
    }

    private static IReadOnlyList<MarketPoint> MarketPointsOf(IReadOnlyList<QuotePointDto> points) =>
        [.. points.Select(point => new MarketPoint(point.Date, point.Close, point.AdjustedClose))];

    private async Task<Result<List<MarketSeries>>> WithValuationsAsync(List<MarketSeries> series, IReadOnlySet<Guid> held, string currency, DateOnly asOf, CancellationToken cancellationToken)
    {
        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var valued = new List<MarketSeries>();
        var ratesByCurrency = new Dictionary<string, IReadOnlyList<CrossRateDto>>(StringComparer.OrdinalIgnoreCase);
        var from = series.SelectMany(item => item.Points).Select(point => point.Date).DefaultIfEmpty(asOf).Min();
        foreach (var item in series)
        {
            if (item.Role != SeriesRole.Instrument || !held.Contains(item.InstrumentId) || item.Points.Count == 0)
            {
                valued.Add(item);
                continue;
            }
            if (!catalog.TryGetValue(item.InstrumentId, out var instrument))
                return EvaluationErrors.EngineRejected($"No currency for instrument {item.InstrumentId}.");
            if (string.Equals(instrument.TradingCurrency, currency, StringComparison.OrdinalIgnoreCase))
            {
                valued.Add(item with { Points = [.. item.Points.Select(point => point with { ValuationClose = point.AdjustedClose })] });
                continue;
            }
            if (!ratesByCurrency.TryGetValue(instrument.TradingCurrency, out var rates))
            {
                if (!await fxBackfill.EnsureSinceAsync(from, cancellationToken))
                    return EvaluationErrors.EngineRejected($"Cannot load FX history for valuations in {currency}.");
                rates = await fxRates.GetCrossRateHistory(instrument.TradingCurrency, currency, from.AddDays(-7), asOf, cancellationToken);
                ratesByCurrency[instrument.TradingCurrency] = rates;
            }
            var converted = ValuationPrices.Converted(item.Points, rates.Select(rate => (rate.Date, rate.Rate)).ToList());
            if (converted is null)
                return EvaluationErrors.EngineRejected($"Missing {instrument.TradingCurrency}/{currency} rate for instrument {item.InstrumentId}.");
            valued.Add(item with { Points = converted });
        }
        return valued;
    }

    private async Task<List<UniverseMember>> UniverseAsync(StrategyVersion version, CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);
        var catalog = known.ToDictionary(instrument => instrument.Id);

        return [.. version.Universe.Select(id => UniverseMemberOf(id, catalog))];
    }

    private static UniverseMember UniverseMemberOf(Guid id, Dictionary<Guid, InstrumentDto> catalog) =>
        catalog.TryGetValue(id, out var instrument)
            ? new UniverseMember(id, instrument.Name, instrument.Group)
            : new UniverseMember(id, id.ToString(), null);
}
