using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Tests;

public sealed class SnapshotBuilderTests
{
    private static readonly Guid AccountId = Guid.NewGuid(), Sub = Guid.NewGuid();
    private static readonly DateOnly FirstMovement = new(2026, 3, 2);
    private static readonly DateTimeOffset Now = new(2026, 3, 6, 18, 0, 0, TimeSpan.Zero);

    private readonly Repository _store = new();
    private readonly FxRates _rates = new();

    private sealed record Entry(IJournalCash Cash) : IJournalEntry
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public DateOnly OccurredOn => FirstMovement;
        public IJournalTrade? Trade => null;
        public IJournalTransfer? Transfer => null;
    }

    private sealed record Cash(string Kind, IReadOnlyList<IJournalAmount> Allocations, decimal? SettledAmount, string? SettledCurrency) : IJournalCash;

    private sealed record Money(Guid SubPortfolioId, decimal Amount) : IJournalAmount;

    private sealed class Journals(string cashCurrency) : IAccountJournals
    {
        public Task<AccountJournal?> FindAsync(Guid accountId, CancellationToken cancellationToken)
        {
            IJournalEntry deposit = new Entry(new Cash("Contribution", [new Money(Sub, 100m)], 100m, cashCurrency));
            var journal = new AccountJournal(UserId.Of(Guid.NewGuid()), "PLN", [deposit], []);

            return Task.FromResult<AccountJournal?>(journal);
        }
    }

    private sealed class Repository : IPerformanceRepository
    {
        public RecalculationMarker? Marker { get; private set; }
        public IReadOnlyList<ValuationSnapshot>? Stored { get; private set; }

        public Task<RecalculationMarker> MarkerAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(Marker ??= RecalculationMarker.For(accountId, ownerId));

        public Task<IReadOnlyList<RecalculationMarker>> MarkersAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecalculationMarker>>(Marker is null ? [] : [Marker]);

        public Task ReplaceSnapshotsAsync(Guid accountId, IReadOnlyList<ValuationSnapshot> snapshots, CancellationToken cancellationToken)
        {
            Stored = snapshots;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ValuationSnapshot>> SnapshotsAsync(
            Guid accountId, SnapshotScenario scenario, DateOnly from, CancellationToken cancellationToken) =>
            Task.FromResult(Stored ?? []);

        public Task<IReadOnlyList<PlannedReport>> PlannedReportsAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlannedReport>>([]);

        public Task<PlannedReport?> FindPlannedReportAsync(Guid reportId, CancellationToken cancellationToken) =>
            Task.FromResult<PlannedReport?>(null);

        public void AddPlannedReport(PlannedReport report) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FxRates : IFxRates, IFxRateBackfill
    {
        public Dictionary<string, decimal> LoadedByBackfill { get; } = [];
        public List<DateOnly> BackfilledSince { get; } = [];
        private readonly Dictionary<string, decimal> _stored = [];

        public Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken)
        {
            BackfilledSince.Add(since);
            foreach (var (currency, rate) in LoadedByBackfill)
                _stored[currency] = rate;

            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<CrossRateDto>> GetCrossRateHistory(
            string fromCurrency, string toCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CrossRateDto>>(
                _stored.TryGetValue(fromCurrency, out var rate) ? [new CrossRateDto(FirstMovement, rate)] : []);

        public Task<FxRateDto?> GetFxRate(string baseCurrency, string quoteCurrency, DateOnly transactionDate, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(
            string baseCurrency, string quoteCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<decimal?> GetCrossRate(string fromCurrency, string toCurrency, DateOnly asOf, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class NoQuotes : IQuoteHistory, IInstrumentCatalog
    {
        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuoteSeriesDto>>([]);

        public Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistoryWithArchive(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuoteSeriesDto>>([]);

        public Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(
            IReadOnlyCollection<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LatestPriceDto>>([]);

        public Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InstrumentDto>>([]);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private SnapshotBuilder Builder(string cashCurrency)
    {
        var noQuotes = new NoQuotes();
        var prices = new PriceBook(noQuotes, noQuotes, _rates, _rates);

        return new SnapshotBuilder(new Journals(cashCurrency), prices, _store, new FixedClock());
    }

    [Fact]
    public async Task Rates_are_loaded_from_the_first_movement_so_a_history_older_than_the_stored_rates_is_still_valued()
    {
        _rates.LoadedByBackfill["USD"] = 4m;

        var built = await Builder("USD").EnsureFreshAsync(AccountId, TestContext.Current.CancellationToken);

        built.IsSuccess.ShouldBeTrue();
        _rates.BackfilledSince.ShouldBe([FirstMovement]);
        _store.Stored.ShouldNotBeNull().Where(snapshot => snapshot.SubPortfolioId is null).Select(snapshot => snapshot.Value).ShouldAllBe(value => value == 400m);
        _store.Marker.ShouldNotBeNull().IsDirty.ShouldBeFalse();
    }

    [Fact]
    public async Task Cash_in_a_currency_without_any_rate_fails_the_valuation_and_leaves_no_snapshots_that_look_current()
    {
        var built = await Builder("CHF").EnsureFreshAsync(AccountId, TestContext.Current.CancellationToken);

        built.IsFailure.ShouldBeTrue();
        built.Error.Code.ShouldBe("valuation.no_exchange_rate");
        _store.Stored.ShouldNotBeNull().ShouldBeEmpty();
        _store.Marker.ShouldNotBeNull().IsDirty.ShouldBeTrue();
    }
}
