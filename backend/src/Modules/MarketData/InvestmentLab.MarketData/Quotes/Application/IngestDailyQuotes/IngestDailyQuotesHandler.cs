using InvestmentLab.SharedKernel;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

public sealed class IngestDailyQuotesHandler(IQuoteStore store, IQuoteProvider provider, TimeProvider clock) : IHandler
{
    public const int InitialHistoryYears = 3;
    private const int SymbolsPerRequest = 20;

    public async Task<IngestReport> Handle(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var instruments = await store.InstrumentsQuotedBy(provider.Provider, cancellationToken);
        var fetchStarts = await store.FetchStartDatesAsync(cancellationToken);

        var tally = new IngestTally();

        foreach (var (from, chunk) in BatchesOf(instruments, fetchStarts, today))
            await FetchBatchAsync(from, chunk, tally, now, cancellationToken);

        if (tally.Ingested.Count > 0)
            store.Record(new DailyQuotesIngested(tally.Ingested, tally.FirstDate, now));

        await store.SaveChangesAsync(cancellationToken);

        return new IngestReport(instruments.Count, tally.Added, tally.Updated, tally.Missing);
    }

    private static IEnumerable<(DateOnly From, QuotedInstrument[] Instruments)> BatchesOf(
        IReadOnlyList<QuotedInstrument> instruments, IReadOnlyDictionary<InstrumentId, DateOnly> fetchStarts, DateOnly today) =>
        instruments
            .GroupBy(instrument => fetchStarts.TryGetValue(instrument.Id, out var start) ? start : today.AddYears(-InitialHistoryYears))
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

            var stored = await StoreSeriesAsync(instrument.Id, from, series, now, cancellationToken);

            tally.Count(instrument, stored);
        }
    }

    private async Task<StoredSeries> StoreSeriesAsync(
        InstrumentId instrumentId, DateOnly from, FetchedSeries series, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stored = (await store.QuotesFromAsync(instrumentId, from, cancellationToken)).ToDictionary(quote => quote.TradingDate);
        var fresh = new List<DailyQuote>();
        var updated = new List<DateOnly>();

        foreach (var point in series.Quotes.Where(point => point.Date >= from).OrderBy(point => point.Date))
        {
            if (point is not { Close: > 0m and var close, AdjustedClose: > 0m and var adjustedClose })
                continue;

            var range = DayRange.Of(point.Open, point.High, point.Low, close);

            if (stored.TryGetValue(point.Date, out var existing))
            {
                if (existing.Refresh(close, adjustedClose, provider.Provider.Code, now))
                    updated.Add(point.Date);

                existing.UseRange(range);

                continue;
            }

            var quote = DailyQuote.Fetched(instrumentId, point.Date, close, adjustedClose, range, provider.Provider.Code, now);
            fresh.Add(quote);
            store.Add(quote);
        }

        if (fresh.Count > 0)
            QuoteQualityInspector.Inspect(fresh, await store.QuoteBeforeAsync(instrumentId, fresh[0].TradingDate, cancellationToken));

        return new StoredSeries(fresh.Count, updated.Count, FirstChange(fresh, updated));
    }

    private static DateOnly? FirstChange(List<DailyQuote> fresh, List<DateOnly> updated)
    {
        var changedDates = fresh.Select(quote => quote.TradingDate).Concat(updated).ToList();

        return changedDates.Count == 0 ? null : changedDates.Min();
    }

    private sealed record StoredSeries(int Added, int Updated, DateOnly? FirstChange);

    private sealed class IngestTally
    {
        public int Added { get; private set; }
        public int Updated { get; private set; }
        public List<string> Missing { get; } = [];
        public List<Guid> Ingested { get; } = [];
        public DateOnly FirstDate { get; private set; } = DateOnly.MaxValue;

        public void Count(QuotedInstrument instrument, StoredSeries stored)
        {
            if (stored.FirstChange is not { } firstChange)
                return;

            Added += stored.Added;
            Updated += stored.Updated;
            Ingested.Add(instrument.Id.Value);
            FirstDate = FirstDate < firstChange ? FirstDate : firstChange;
        }
    }
}
