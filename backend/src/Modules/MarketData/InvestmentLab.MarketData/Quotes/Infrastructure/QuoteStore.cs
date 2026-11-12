using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class QuoteStore(MarketDataDbContext db) : IQuoteStore
{
    public async Task<IReadOnlyList<QuotedInstrument>> InstrumentsQuotedBy(QuoteProvider provider, CancellationToken cancellationToken)
    {
        var instruments = await db.Instruments.AsNoTracking()
            .Where(instrument => instrument.ProviderSymbols.Any(symbol => symbol.Provider == provider))
            .ToListAsync(cancellationToken);

        return QuotedBy(provider, instruments);
    }

    private static List<QuotedInstrument> QuotedBy(QuoteProvider provider, List<Instrument> instruments) =>
        [.. instruments.Select(instrument => new QuotedInstrument(
            instrument.Id,
            instrument.SymbolFor(provider)!,
            instrument.TradingCurrency))];

    public Task<bool> InstrumentExistsAsync(InstrumentId id, CancellationToken cancellationToken) =>
        db.Instruments.AnyAsync(instrument => instrument.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<InstrumentId, DateOnly>> FetchStartDatesAsync(CancellationToken cancellationToken)
    {
        var starts = await db.DailyQuotes
            .GroupBy(quote => quote.InstrumentId)
            .Select(group => new
            {
                InstrumentId = group.Key,
                Last = group.Max(quote => quote.TradingDate),
                FirstWithoutRange = group
                    .Where(quote => quote.Open == null && (quote.QualityFlags & QualityFlags.Corrected) == 0)
                    .Min(quote => (DateOnly?)quote.TradingDate),
            })
            .ToListAsync(cancellationToken);

        return starts.ToDictionary(row => row.InstrumentId, row => row.FirstWithoutRange ?? row.Last);
    }

    public async Task<IReadOnlyDictionary<InstrumentId, FirstStoredQuote>> FirstQuotesAsync(
        IReadOnlyCollection<InstrumentId> instrumentIds, CancellationToken cancellationToken)
    {
        var first = await db.DailyQuotes.AsNoTracking()
            .Where(quote => instrumentIds.Contains(quote.InstrumentId))
            .GroupBy(quote => quote.InstrumentId)
            .Select(group => group.OrderBy(quote => quote.TradingDate).First())
            .ToListAsync(cancellationToken);

        return first.ToDictionary(quote => quote.InstrumentId, quote => new FirstStoredQuote(quote.TradingDate, quote.AdjustedClose));
    }

    public async Task<IReadOnlyList<DailyQuote>> QuotesFromAsync(InstrumentId id, DateOnly from, CancellationToken cancellationToken) =>
        await db.DailyQuotes
            .Where(quote => quote.InstrumentId == id && quote.TradingDate >= from)
            .OrderBy(quote => quote.TradingDate)
            .ToListAsync(cancellationToken);

    public Task<DailyQuote?> QuoteBeforeAsync(InstrumentId id, DateOnly date, CancellationToken cancellationToken) =>
        db.DailyQuotes.AsNoTracking()
            .Where(quote => quote.InstrumentId == id && quote.TradingDate < date)
            .OrderByDescending(quote => quote.TradingDate)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(DailyQuote quote) => db.DailyQuotes.Add(quote);

    public void Add(QuoteCorrection correction) => db.QuoteCorrections.Add(correction);

    public void Record(IDomainEvent domainEvent) => db.Record(domainEvent);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
