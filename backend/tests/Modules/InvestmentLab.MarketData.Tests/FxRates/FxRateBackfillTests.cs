using InvestmentLab.MarketData.FxRates.Application.BackfillFxRates;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
using InvestmentLab.MarketData.FxRates.Application.Shared;
using InvestmentLab.MarketData.FxRates.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace InvestmentLab.MarketData.Tests.FxRates;

public sealed class FxRateBackfillTests
{
    private static readonly DateTimeOffset Saturday = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Friday = new(2026, 11, 6);
    private static readonly DateOnly FirstStored = new(2024, 1, 2);

    [Fact]
    public async Task A_second_backfill_on_a_weekend_does_not_ask_the_source_again()
    {
        var clock = new FakeTimeProvider(Saturday);
        var provider = new CountingProvider();
        var checks = new FxRateChecks(clock);

        await BackfillOf(clock, provider, checks).EnsureSinceAsync(FirstStored, CancellationToken.None);
        var afterFirst = provider.Calls;
        await BackfillOf(clock, provider, checks).EnsureSinceAsync(FirstStored, CancellationToken.None);

        afterFirst.ShouldBeGreaterThan(0);
        provider.Calls.ShouldBe(afterFirst);
    }

    [Fact]
    public async Task New_rates_are_asked_for_again_after_an_hour()
    {
        var clock = new FakeTimeProvider(Saturday);
        var provider = new CountingProvider();
        var checks = new FxRateChecks(clock);

        await BackfillOf(clock, provider, checks).EnsureSinceAsync(FirstStored, CancellationToken.None);
        var afterFirst = provider.Calls;
        clock.Advance(TimeSpan.FromMinutes(61));
        await BackfillOf(clock, provider, checks).EnsureSinceAsync(FirstStored, CancellationToken.None);

        provider.Calls.ShouldBe(afterFirst + IngestFxRatesHandler.Currencies.Length);
    }

    private static BackfillFxRatesHandler BackfillOf(TimeProvider clock, IFxRateProvider provider, FxRateChecks checks)
    {
        var store = new StoredRates();
        var ingest = new IngestFxRatesHandler(store, provider, clock);

        return new BackfillFxRatesHandler(store, provider, ingest, checks, clock, NullLogger<BackfillFxRatesHandler>.Instance);
    }

    private sealed class CountingProvider : IFxRateProvider
    {
        public int Calls { get; private set; }

        public FxRateSource Source => FxRateSource.Nbp;

        public Task<IReadOnlyList<FxRate>> FetchAsync(Currency @base, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult<IReadOnlyList<FxRate>>([]);
        }
    }

    private sealed class StoredRates : IFxRateStore
    {
        public Task<DateOnly?> LastEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(Friday);

        public Task<DateOnly?> FirstEffectiveDateAsync(FxRateSource source, Currency @base, Currency quote, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(FirstStored);

        public Task<IReadOnlyList<FxRate>> RatesBetweenAsync(Currency @base, Currency quote, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FxRate>> LatestToPivotAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Add(FxRate rate)
        {
        }

        public void Record(IDomainEvent domainEvent)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
