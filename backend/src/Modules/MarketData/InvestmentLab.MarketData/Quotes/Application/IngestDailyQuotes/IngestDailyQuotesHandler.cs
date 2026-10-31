using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

public sealed class IngestDailyQuotesHandler(IQuoteStore store, IQuoteProvider provider, TimeProvider clock)
{
    public const int InitialHistoryYears = 3;
    private const int SymbolsPerRequest = 20;

    public async Task<IngestReport> Handle(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var instruments = await store.InstrumentsQuotedBy(provider.Provider, cancellationToken);
        var lastDates = await store.LastTradingDatesAsync(cancellationToken);

        var tally = new IngestTally();

        foreach (var (from, chunk) in BatchesOf(instruments, lastDates, today))
            await FetchBatchAsync(from, chunk, tally, now, cancellationToken);

        if (tally.Ingested.Count > 0)
            store.Record(new DailyQuotesIngested(tally.Ingested, tally.FirstDate, now));

        await store.SaveChangesAsync(cancellationToken);

        return new IngestReport(instruments.Count, tally.Added, tally.Updated, tally.Missing);
    }

    private static IEnumerable<(DateOnly From, QuotedInstrument[] Instruments)> BatchesOf(
        IReadOnlyList<QuotedInstrument> instruments, IReadOnlyDictionary<InstrumentId, DateOnly> lastDates, DateOnly today) =>
        instruments
            .GroupBy(instrument => lastDates.TryGetValue(instrument.Id, out var last) ? last : today.AddYears(-InitialHistoryYears))
            .Where(group => group.Key <= today)
            .SelectMany(group => group.Chunk(SymbolsPerRequest).Select(chunk => (From: group.Key, Instruments: chunk)));

    private async Task FetchBatchAsync(
        DateOnly from, QuotedInstrument[] chunk, IngestTally tally, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var bySymbol = chunk.ToDictionary(instrument => instrument.Symbol.Symbol);
        var fetched = await provider.FetchAsync([.. bySymbol.Keys], from, cancellationToken);

        tally.Missing.AddRange(fetched.Missing);

        foreach (var series in fetched.Series)
        {
            if (!bySymbol.TryGetValue(series.Symbol, out var instrument))
                continue;

            var (added, updated) = await StoreSeriesAsync(instrument.Id, from, series, now, cancellationToken);

            tally.Count(instrument, from, added, updated);
        }
    }

    private async Task<(int Added, int Updated)> StoreSeriesAsync(
        InstrumentId instrumentId, DateOnly from, FetchedSeries series, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stored = (await store.QuotesFromAsync(instrumentId, from, cancellationToken)).ToDictionary(quote => quote.TradingDate);
        var fresh = new List<DailyQuote>();
        var updated = 0;

        foreach (var point in series.Quotes.Where(point => point.Date >= from).OrderBy(point => point.Date))
        {
            if (point is not { Close: > 0m and var close, AdjustedClose: > 0m and var adjustedClose })
                continue;

            if (stored.TryGetValue(point.Date, out var existing))
            {
                if (existing.Refresh(close, adjustedClose, provider.Provider.Code, now))
                    updated++;

                continue;
            }

            var quote = DailyQuote.Fetched(instrumentId, point.Date, close, adjustedClose, provider.Provider.Code, now);
            fresh.Add(quote);
            store.Add(quote);
        }

        if (fresh.Count > 0)
            QuoteQualityInspector.Inspect(fresh, await store.QuoteBeforeAsync(instrumentId, fresh[0].TradingDate, cancellationToken));

        return (fresh.Count, updated);
    }

    private sealed class IngestTally
    {
        public int Added { get; private set; }
        public int Updated { get; private set; }
        public List<string> Missing { get; } = [];
        public List<Guid> Ingested { get; } = [];
        public DateOnly FirstDate { get; private set; } = DateOnly.MaxValue;

        public void Count(QuotedInstrument instrument, DateOnly from, int added, int updated)
        {
            if (added + updated == 0)
                return;

            Added += added;
            Updated += updated;
            Ingested.Add(instrument.Id.Value);
            FirstDate = FirstDate < from ? FirstDate : from;
        }
    }
}
