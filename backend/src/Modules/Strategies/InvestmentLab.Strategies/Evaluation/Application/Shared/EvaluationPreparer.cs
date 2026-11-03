using InvestmentLab.Signals.Contracts;
using InvestmentLab.Strategies.Evaluation.Domain;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed class EvaluationPreparer(IStrategyReader strategies, IQuoteHistory quotes, IInstrumentCatalog instruments, IMarketSignalStates marketSignals)
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
        var series = await MarketSeriesAsync(instrumentIds, version, request.AsOf, cancellationToken);
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
        List<Guid> instrumentIds, StrategyVersion version, DateOnly asOf, CancellationToken cancellationToken)
    {
        var history = await HistoryAsync(instrumentIds, version, asOf, cancellationToken);
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
        List<Guid> instrumentIds, StrategyVersion version, DateOnly asOf, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> volatilityIndex = version.VolatilityIndexInstrumentId is { } id ? [id] : [];
        var marketIds = volatilityIndex.Prepend(version.BenchmarkInstrumentId).Concat(version.ExtraSeries);
        Guid[] requestedIds = [.. instrumentIds.Concat(marketIds).Distinct()];

        var history = await quotes.GetQuoteHistoryWithArchive(requestedIds, asOf.AddDays(-HistoryDays), asOf, cancellationToken);

        return history.ToDictionary(series => series.InstrumentId, series => MarketPointsOf(series.Points));
    }

    private static IReadOnlyList<MarketPoint> MarketPointsOf(IReadOnlyList<QuotePointDto> points) =>
        [.. points.Select(point => new MarketPoint(point.Date, point.Close, point.AdjustedClose))];

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
