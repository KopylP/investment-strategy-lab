using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;

/// <summary>
/// Builds the evaluation context (strategy version, quotes up to <c>asOf</c>, sub-portfolio state) and asks the engine
/// for a decision or a risk assessment. Strategies keep no state between runs yet (no engine supports it).
/// </summary>
public sealed class EvaluateStrategyHandler(IStrategyReader strategies, IStrategyEngine engine, IQuoteHistory quotes, IInstrumentCatalog instruments)
{
    /// <summary>Two years: a 200-day average and twelve-month returns with room for holidays.</summary>
    private const int HistoryDays = 2 * 365 + 14;

    public async Task<Result<StrategyDecisionDto>> Decide(EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (strategy, version, context) = prepared.Value;
        var decision = await engine.DecideAsync(strategy.TypeCode, context, cancellationToken);
        return decision.IsSuccess
            ? decision.Value with { StrategyId = strategy.Id.Value, StrategyVersion = version.Number, CashInstrumentId = context.CashInstrumentId }
            : decision.Error;
    }

    public async Task<Result<RiskAssessmentDto>> AssessRisk(EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (strategy, version, context) = prepared.Value;
        var assessment = await engine.AssessRiskAsync(strategy.TypeCode, context, cancellationToken);
        return assessment.IsSuccess ? assessment.Value with { StrategyId = strategy.Id.Value, StrategyVersion = version.Number } : assessment.Error;
    }

    private async Task<Result<(Strategy Strategy, StrategyVersion Version, StrategyEvaluationContext Context)>> PrepareAsync(
        EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        if (request.StrategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(request.StrategyId), cancellationToken) is not { } strategy)
            return StrategyErrors.NotFound(request.StrategyId);
        if (strategy.Version(request.Version) is not { } version)
            return StrategyErrors.VersionNotFound(request.StrategyId, request.Version!.Value);

        var cashInstrumentId = request.CashInstrumentId ?? version.CashInstrumentId;
        var held = request.SubPortfolio.Holdings.Select(holding => holding.InstrumentId);
        var instrumentIds = version.Universe.Concat(held).Concat(cashInstrumentId is { } cash ? [cash] : []).Distinct().ToList();
        Guid[] marketIds = [version.BenchmarkInstrumentId, .. version.VolatilityIndexInstrumentId is { } vix ? [vix] : Array.Empty<Guid>()];

        var history = (await quotes.GetQuoteHistory([.. instrumentIds.Concat(marketIds).Distinct()], request.AsOf.AddDays(-HistoryDays), request.AsOf, cancellationToken))
            .ToDictionary(series => series.InstrumentId, series => series.Points);
        IReadOnlyList<QuotePointDto> PointsOf(Guid id) => history.TryGetValue(id, out var points) ? points : [];

        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var series = instrumentIds.Select(id => new MarketSeries(id, SeriesRole.Instrument, PointsOf(id)))
            .Append(new MarketSeries(version.BenchmarkInstrumentId, SeriesRole.Benchmark, PointsOf(version.BenchmarkInstrumentId)))
            .Concat(version.VolatilityIndexInstrumentId is { } volatility
                ? [new MarketSeries(volatility, SeriesRole.VolatilityIndex, PointsOf(volatility))]
                : [])
            .ToList();

        var context = new StrategyEvaluationContext(
            request.AsOf,
            version.Parameters,
            [.. version.Universe.Select(id => catalog.TryGetValue(id, out var instrument)
                ? new UniverseMember(id, instrument.Name, instrument.Group)
                : new UniverseMember(id, id.ToString(), null))],
            cashInstrumentId,
            series,
            request.SubPortfolio,
            request.Contribution);

        return (strategy, version, context);
    }
}
