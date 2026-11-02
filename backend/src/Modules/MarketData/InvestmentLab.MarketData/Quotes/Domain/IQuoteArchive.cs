using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Domain;

public interface IQuoteArchive
{
    Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> OlderThanStoredAsync(
        IReadOnlyCollection<InstrumentId> instrumentIds, CancellationToken cancellationToken);
}
