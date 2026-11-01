using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.BackfillQuoteHistory;

public sealed class BackfillQuoteHistoryHandler(IQuoteStore store, IQuoteProvider provider, TimeProvider clock)
{
    public const int FullHistoryYears = 20;
    private const int SymbolsPerRequest = 5;

    public async Task<int> Handle(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var from = DateOnly.FromDateTime(now.UtcDateTime).AddYears(-FullHistoryYears);
        var instruments = await store.InstrumentsWithoutFullHistory(provider.Provider, cancellationToken);

        var added = 0;

        foreach (var chunk in instruments.Chunk(SymbolsPerRequest))
            added += await BackfillAsync(chunk, from, now, cancellationToken);

        if (added > 0)
        {
            store.Record(new QuoteHistoryExtended(now));
            await store.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private async Task<int> BackfillAsync(QuotedInstrument[] chunk, DateOnly from, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var bySymbol = chunk.ToDictionary(instrument => instrument.Symbol.Symbol);
        var fetched = await provider.FetchAsync([.. bySymbol.Keys], from, cancellationToken);

        var added = 0;

        foreach (var series in fetched.Series)
        {
            if (!bySymbol.TryGetValue(series.Symbol, out var instrument))
                continue;

            added += await AddOlderQuotesAsync(instrument, series, from, now, cancellationToken);
            await store.MarkFullHistoryFetchedAsync(instrument.Id, now, cancellationToken);
        }

        await store.SaveChangesAsync(cancellationToken);

        return added;
    }

    private async Task<int> AddOlderQuotesAsync(
        QuotedInstrument instrument, FetchedSeries series, DateOnly from, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var firstStored = await store.FirstTradingDateAsync(instrument.Id, cancellationToken);
        var older = series.Quotes
            .Where(point => point.Date >= from && (firstStored is null || point.Date < firstStored))
            .OrderBy(point => point.Date);

        var fresh = new List<DailyQuote>();

        foreach (var point in older)
        {
            if (point is not { Close: > 0m and var close, AdjustedClose: > 0m and var adjustedClose })
                continue;

            fresh.Add(DailyQuote.Fetched(instrument.Id, point.Date, close, adjustedClose, provider.Provider.Code, now));
        }

        QuoteQualityInspector.Inspect(fresh, previous: null);
        fresh.ForEach(store.Add);

        return fresh.Count;
    }
}
