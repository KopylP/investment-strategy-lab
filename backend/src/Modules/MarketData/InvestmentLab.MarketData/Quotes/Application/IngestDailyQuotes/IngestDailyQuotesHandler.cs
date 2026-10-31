using InvestmentLab.MarketData.Instruments.Domain;
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

        var (added, updated, missing) = (0, 0, new List<string>());
        var ingested = new List<Guid>();
        var firstDate = DateOnly.MaxValue;

        var batches = instruments
            .GroupBy(instrument => lastDates.TryGetValue(instrument.Id, out var last) ? last : today.AddYears(-InitialHistoryYears))
            .Where(group => group.Key <= today)
            .SelectMany(group => group.Chunk(SymbolsPerRequest).Select(chunk => (From: group.Key, Instruments: chunk)));

        foreach (var (from, chunk) in batches)
        {
            var bySymbol = chunk.ToDictionary(instrument => instrument.Symbol.Symbol);
            var fetched = await provider.FetchAsync([.. bySymbol.Keys], from, cancellationToken);
            missing.AddRange(fetched.Missing);

            foreach (var series in fetched.Series)
            {
                if (!bySymbol.TryGetValue(series.Symbol, out var instrument))
                    continue;

                var (seriesAdded, seriesUpdated) = await StoreSeriesAsync(instrument.Id, from, series, now, cancellationToken);

                if (seriesAdded + seriesUpdated == 0)
                    continue;

                (added, updated) = (added + seriesAdded, updated + seriesUpdated);
                ingested.Add(instrument.Id.Value);
                firstDate = firstDate < from ? firstDate : from;
            }
        }

        if (ingested.Count > 0)
            store.Record(new DailyQuotesIngested(ingested, firstDate, now));

        await store.SaveChangesAsync(cancellationToken);

        return new IngestReport(instruments.Count, added, updated, missing);
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
}
