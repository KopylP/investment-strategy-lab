using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application;
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
        return [.. instruments.Select(instrument => new QuotedInstrument(instrument.Id, instrument.SymbolFor(provider)!, instrument.TradingCurrency))];
    }

    public Task<bool> InstrumentExistsAsync(InstrumentId id, CancellationToken cancellationToken) =>
        db.Instruments.AnyAsync(instrument => instrument.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<InstrumentId, DateOnly>> LastTradingDatesAsync(CancellationToken cancellationToken) =>
        await db.DailyQuotes
            .GroupBy(quote => quote.InstrumentId)
            .Select(group => new { InstrumentId = group.Key, Last = group.Max(quote => quote.TradingDate) })
            .ToDictionaryAsync(row => row.InstrumentId, row => row.Last, cancellationToken);

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
