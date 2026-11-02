using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class QuoteArchive(IQuoteStore store, IQuoteProvider provider, QuoteArchiveCache cache, TimeProvider clock) : IQuoteArchive
{
    public const int YearsReached = 25;
    private const int SymbolsPerRequest = 5;

    public Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> OlderThanStoredAsync(
        IReadOnlyCollection<InstrumentId> instrumentIds, CancellationToken cancellationToken) =>
        cache.GetOrLoadAsync(instrumentIds, LoadAsync, cancellationToken);

    private async Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> LoadAsync(
        IReadOnlyList<InstrumentId> instrumentIds, CancellationToken cancellationToken)
    {
        var quoted = await QuotedAsync(instrumentIds, cancellationToken);
        var firstStored = await store.FirstQuotesAsync(instrumentIds, cancellationToken);
        var from = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddYears(-YearsReached);

        var loaded = instrumentIds.ToDictionary(id => id, _ => (IReadOnlyList<ArchivedPrice>)[]);

        foreach (var chunk in quoted.Chunk(SymbolsPerRequest))
        {
            foreach (var (instrumentId, archived) in await FetchAsync(chunk, from, cancellationToken))
                loaded[instrumentId] = OlderQuotes.Before(archived, firstStored.GetValueOrDefault(instrumentId));
        }

        return loaded;
    }

    private async Task<List<QuotedInstrument>> QuotedAsync(IReadOnlyList<InstrumentId> instrumentIds, CancellationToken cancellationToken)
    {
        var quoted = await store.InstrumentsQuotedBy(provider.Provider, cancellationToken);

        return [.. quoted.Where(instrument => instrumentIds.Contains(instrument.Id))];
    }

    private async Task<Dictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> FetchAsync(
        QuotedInstrument[] chunk, DateOnly from, CancellationToken cancellationToken)
    {
        var bySymbol = chunk.ToDictionary(instrument => instrument.Symbol.Symbol, instrument => instrument.Id);
        var fetched = await provider.FetchAsync([.. bySymbol.Keys], from, cancellationToken);
        var known = fetched.Series.Where(series => bySymbol.ContainsKey(series.Symbol));

        return known.ToDictionary(series => bySymbol[series.Symbol], series => PricesOf(series));
    }

    private static IReadOnlyList<ArchivedPrice> PricesOf(FetchedSeries series)
    {
        var complete = series.Quotes.Where(quote => quote is { Close: > 0m, AdjustedClose: > 0m });

        return [.. complete.OrderBy(quote => quote.Date).Select(quote => new ArchivedPrice(quote.Date, quote.Close!.Value, quote.AdjustedClose!.Value))];
    }
}
