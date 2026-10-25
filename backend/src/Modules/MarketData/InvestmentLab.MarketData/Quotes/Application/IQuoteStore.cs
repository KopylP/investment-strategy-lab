using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application;

public sealed record QuotedInstrument(InstrumentId Id, ProviderSymbol Symbol, Currency TradingCurrency);

public interface IQuoteStore
{
    Task<IReadOnlyList<QuotedInstrument>> InstrumentsQuotedBy(QuoteProvider provider, CancellationToken cancellationToken);

    Task<bool> InstrumentExistsAsync(InstrumentId id, CancellationToken cancellationToken);

    /// <summary>The date of the newest stored quote per instrument.</summary>
    Task<IReadOnlyDictionary<InstrumentId, DateOnly>> LastTradingDatesAsync(CancellationToken cancellationToken);

    /// <summary>Stored quotes of an instrument on or after <paramref name="from"/>, oldest first.</summary>
    Task<IReadOnlyList<DailyQuote>> QuotesFromAsync(InstrumentId id, DateOnly from, CancellationToken cancellationToken);

    /// <summary>The newest stored quote before <paramref name="date"/>.</summary>
    Task<DailyQuote?> QuoteBeforeAsync(InstrumentId id, DateOnly date, CancellationToken cancellationToken);

    void Add(DailyQuote quote);

    void Add(QuoteCorrection correction);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
