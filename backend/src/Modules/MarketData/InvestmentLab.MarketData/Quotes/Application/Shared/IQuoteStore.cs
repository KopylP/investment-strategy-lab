using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public interface IQuoteStore
{
    Task<IReadOnlyList<QuotedInstrument>> InstrumentsQuotedBy(QuoteProvider provider, CancellationToken cancellationToken);

    Task<bool> InstrumentExistsAsync(InstrumentId id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<InstrumentId, DateOnly>> FetchStartDatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<InstrumentId, FirstStoredQuote>> FirstQuotesAsync(IReadOnlyCollection<InstrumentId> instrumentIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyQuote>> QuotesFromAsync(InstrumentId id, DateOnly from, CancellationToken cancellationToken);

    Task<DailyQuote?> QuoteBeforeAsync(InstrumentId id, DateOnly date, CancellationToken cancellationToken);

    void Add(DailyQuote quote);

    void Add(QuoteCorrection correction);

    void Record(IDomainEvent domainEvent);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
