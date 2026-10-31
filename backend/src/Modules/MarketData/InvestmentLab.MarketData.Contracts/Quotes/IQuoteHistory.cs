namespace InvestmentLab.MarketData.Contracts.Quotes;

public interface IQuoteHistory
{
    Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken);
}
