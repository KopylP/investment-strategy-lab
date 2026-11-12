using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Infrastructure;

namespace InvestmentLab.Strategies.Tests.Evaluation;

public sealed class EvaluationPreparerTests
{
    private static readonly Guid Fund = Guid.NewGuid(), Benchmark = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 6);

    [Fact]
    public async Task Long_lookbacks_request_enough_calendar_history_and_keep_signal_prices_separate_from_valuations()
    {
        var quotes = new Quotes();
        var fx = new Rates();
        var strategy = Strategy.Create(UserId.New(), "Long rules", "rule-based",
            new StrategySetup("{}", [Fund], null, new MarketSeriesRefs(Benchmark, null)), DateTimeOffset.UtcNow).Value;
        var preparer = new EvaluationPreparer(new Reader(strategy), quotes, new Catalog(), new Signals(), new Checks(), fx, fx);
        var request = new EvaluateStrategyRequest(strategy.Id.Value, 1,
            new SubPortfolioStateDto("EUR", 0, [new HoldingStateDto(Fund, [new LotStateDto(1, 80, Day.AddDays(-1))])]),
            0, Day, ValuationCurrency: "EUR");

        var prepared = await preparer.PrepareAsync(request, CancellationToken.None);

        prepared.IsSuccess.ShouldBeTrue();
        quotes.From.ShouldBeLessThanOrEqualTo(Day.AddDays(-2018));
        var context = prepared.Value.Context;
        var point = context.MarketSeries.Single(series => series.Role == SeriesRole.Instrument).Points[^1];
        point.AdjustedClose.ShouldBe(90m);
        point.ValuationClose.ShouldBe(72m);
        context.SubPortfolio.Holdings[0].Lots[0].UnitCost.ShouldBe(80m);
        var wire = PythonStrategyEngine.ToEngineContext(context).MarketSeries.Single(series => series.Role == "instrument");
        wire.AdjustedClose[^1].ShouldBe(90m);
        wire.ValuationClose![^1].ShouldBe(72m);
    }

    [Fact]
    public void Valuation_conversion_never_uses_future_or_indefinitely_stale_exchange_rates()
    {
        MarketPoint[] points = [new(Day, 90m, 90m)];
        ValuationPrices.Converted(points, [(Day.AddDays(1), .8m)]).ShouldBeNull();
        ValuationPrices.Converted(points, [(Day.AddDays(-8), .8m)]).ShouldBeNull();
        ValuationPrices.Converted(points, [(Day.AddDays(-3), .8m)])![0].ValuationClose.ShouldBe(72m);
    }

    private sealed class Reader(Strategy strategy) : IStrategyReader
    {
        public Task<Strategy?> FindAsync(StrategyId id, CancellationToken cancellationToken) => Task.FromResult<Strategy?>(strategy);
    }

    private sealed class Checks : IParameterChecks
    {
        public Task<Result<ParametersCheck>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken) =>
            Task.FromResult<Result<ParametersCheck>>(new ParametersCheck([], [], 1009));
    }

    private sealed class Catalog : IInstrumentCatalog
    {
        public Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InstrumentDto>>([new(Fund, "ETF", null, "USD fund", "NYSE", "USD", null, true, []),
                new(Benchmark, "ETF", null, "Benchmark", "NYSE", "USD", null, true, [])]);
    }

    private sealed class Signals : IMarketSignalStates
    {
        public Task<IReadOnlyList<MarketSignalStateDto>> GetStatesAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MarketSignalStateDto>>([]);
    }

    private sealed class Quotes : IQuoteHistory
    {
        public DateOnly From { get; private set; }
        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistoryWithArchive(IReadOnlyCollection<Guid> ids, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            From = from;
            return Task.FromResult<IReadOnlyList<QuoteSeriesDto>>([.. ids.Select(id => new QuoteSeriesDto(id,
                [new(Day.AddDays(-1), 100m, 100m, []), new(Day, 90m, 90m, [])]))]);
        }
        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(IReadOnlyCollection<Guid> ids, DateOnly from, DateOnly to, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(IReadOnlyCollection<Guid> ids, DateOnly day, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Rates : IFxRates, IFxRateBackfill
    {
        public Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<CrossRateDto>> GetCrossRateHistory(string from, string to, DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CrossRateDto>>([new(Day.AddDays(-1), .8m)]);
        public Task<decimal?> GetCrossRate(string from, string to, DateOnly day, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FxRateDto?> GetFxRate(string from, string to, DateOnly day, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(string from, string to, DateOnly start, DateOnly end, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
