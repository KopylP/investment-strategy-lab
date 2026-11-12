using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class QuoteHistory(MarketDataDbContext db, IQuoteArchive archive) : IQuoteHistory
{
    private static readonly QualityFlags[] StoredFlags = [QualityFlags.Gap, QualityFlags.Spike, QualityFlags.Corrected];

    public async Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var ids = ToIds(instrumentIds);
        var quotes = await db.DailyQuotes.AsNoTracking()
            .Where(quote => ids.Contains(quote.InstrumentId) && quote.TradingDate >= from && quote.TradingDate <= to)
            .OrderBy(quote => quote.TradingDate)
            .ToListAsync(cancellationToken);

        var byInstrument = quotes.ToLookup(quote => quote.InstrumentId);

        return [.. ids.Select(id => new QuoteSeriesDto(id.Value, [.. byInstrument[id].Select(ToPoint)]))];
    }

    public async Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistoryWithArchive(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var stored = await GetQuoteHistory(instrumentIds, from, to, cancellationToken);
        var startingLater = await StoredOnlyAfterAsync(ToIds(instrumentIds), from, cancellationToken);
        if (startingLater.Count == 0)
            return stored;

        var older = await archive.OlderThanStoredAsync(startingLater, cancellationToken);

        return [.. stored.Select(series => WithOlder(series, older, from, to))];
    }

    public async Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken)
    {
        var ids = ToIds(instrumentIds);
        var latest = await db.DailyQuotes.AsNoTracking()
            .Where(quote => ids.Contains(quote.InstrumentId) && quote.TradingDate <= asOf)
            .GroupBy(quote => quote.InstrumentId)
            .Select(group => group.OrderByDescending(quote => quote.TradingDate).First())
            .ToListAsync(cancellationToken);
        var currencies = await db.Instruments.AsNoTracking()
            .Where(instrument => ids.Contains(instrument.Id))
            .ToDictionaryAsync(instrument => instrument.Id, instrument => instrument.TradingCurrency.Code, cancellationToken);

        return [.. latest.Select(quote => new LatestPriceDto(
            quote.InstrumentId.Value, quote.TradingDate, quote.Close, quote.AdjustedClose, currencies[quote.InstrumentId],
            QuoteQualityInspector.IsStale(quote.TradingDate, asOf)))];
    }

    private async Task<List<InstrumentId>> StoredOnlyAfterAsync(List<InstrumentId> ids, DateOnly from, CancellationToken cancellationToken)
    {
        var reachingBack = await db.DailyQuotes.AsNoTracking()
            .Where(quote => ids.Contains(quote.InstrumentId) && quote.TradingDate <= from)
            .Select(quote => quote.InstrumentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. ids.Except(reachingBack)];
    }

    private static QuoteSeriesDto WithOlder(
        QuoteSeriesDto stored, IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>> older, DateOnly from, DateOnly to)
    {
        if (!older.TryGetValue(InstrumentId.Of(stored.InstrumentId), out var archived))
            return stored;

        var firstStored = stored.Points.Count > 0 ? stored.Points[0].Date : DateOnly.MaxValue;
        var inWindow = archived.Where(price => price.Date >= from && price.Date <= to && price.Date < firstStored);
        var olderPoints = inWindow.Select(price => new QuotePointDto(price.Date, null, null, null, price.Close, price.AdjustedClose, []));

        return stored with { Points = [.. olderPoints, .. stored.Points] };
    }

    private static List<InstrumentId> ToIds(IReadOnlyCollection<Guid> instrumentIds) =>
        [.. instrumentIds.Where(id => id != Guid.Empty).Distinct().Select(InstrumentId.Of)];

    private static QuotePointDto ToPoint(DailyQuote quote) =>
        new(quote.TradingDate, quote.Open, quote.High, quote.Low, quote.Close, quote.AdjustedClose,
            [.. StoredFlags.Where(flag => quote.QualityFlags.HasFlag(flag)).Select(flag => flag.ToString())]);
}
