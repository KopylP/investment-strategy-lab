using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.BackfillQuoteHistory;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;
using Microsoft.Extensions.Time.Testing;

namespace InvestmentLab.MarketData.Tests.Quotes;

public sealed class BackfillQuoteHistoryTests
{
    private static readonly DateOnly FirstStored = new(2024, 1, 10);
    private static readonly QuotedInstrument Fund = new(InstrumentId.New(), ProviderSymbol.TryParse("yfinance:SPY").Value, Currency.USD);

    [Fact]
    public async Task Adds_only_quotes_older_than_what_is_stored_and_does_not_ask_again()
    {
        var store = new FakeStore();
        var handler = HandlerOver(store);

        var added = await handler.Handle(TestContext.Current.CancellationToken);
        var addedAgain = await handler.Handle(TestContext.Current.CancellationToken);

        added.ShouldBe(1);
        addedAgain.ShouldBe(0);
        store.Added.Single().TradingDate.ShouldBe(FirstStored.AddDays(-1));
        store.Events.OfType<QuoteHistoryExtended>().Count().ShouldBe(1);
    }

    [Fact]
    public async Task A_symbol_the_provider_does_not_know_stays_to_be_asked_again()
    {
        var store = new FakeStore();
        var handler = HandlerOver(store, known: false);

        await handler.Handle(TestContext.Current.CancellationToken);

        store.Fetched.ShouldBeEmpty();
    }

    private static BackfillQuoteHistoryHandler HandlerOver(FakeStore store, bool known = true) =>
        new(store, new FakeProvider(known), new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 4, 0, 0, TimeSpan.Zero)));

    private sealed class FakeProvider(bool known) : IQuoteProvider
    {
        public QuoteProvider Provider => QuoteProvider.YFinance;

        public Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken)
        {
            FetchedQuote[] quotes =
            [
                new(FirstStored.AddDays(-1), 100m, 90m),
                new(FirstStored, 101m, 91m),
                new(FirstStored.AddDays(1), 102m, 92m),
            ];
            FetchedSeries[] series = known ? [new(symbols[0], quotes)] : [];
            string[] missing = known ? [] : [symbols[0]];

            return Task.FromResult(new FetchedQuotes(series, missing));
        }
    }

    private sealed class FakeStore : IQuoteStore
    {
        public List<DailyQuote> Added { get; } = [];
        public List<IDomainEvent> Events { get; } = [];
        public List<InstrumentId> Fetched { get; } = [];

        public Task<IReadOnlyList<QuotedInstrument>> InstrumentsWithoutFullHistory(QuoteProvider provider, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuotedInstrument>>(Fetched.Contains(Fund.Id) ? [] : [Fund]);

        public Task<DateOnly?> FirstTradingDateAsync(InstrumentId id, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(FirstStored);

        public Task MarkFullHistoryFetchedAsync(InstrumentId id, DateTimeOffset at, CancellationToken cancellationToken)
        {
            Fetched.Add(id);

            return Task.CompletedTask;
        }

        public void Add(DailyQuote quote) => Added.Add(quote);

        public void Record(IDomainEvent domainEvent) => Events.Add(domainEvent);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<QuotedInstrument>> InstrumentsQuotedBy(QuoteProvider provider, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> InstrumentExistsAsync(InstrumentId id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<InstrumentId, DateOnly>> LastTradingDatesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<DailyQuote>> QuotesFromAsync(InstrumentId id, DateOnly from, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DailyQuote?> QuoteBeforeAsync(InstrumentId id, DateOnly date, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Add(QuoteCorrection correction) => throw new NotSupportedException();
    }
}
