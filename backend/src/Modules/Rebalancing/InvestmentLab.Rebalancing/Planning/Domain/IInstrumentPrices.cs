namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IInstrumentPrices
{
    Task<PricesInCurrency> InCurrencyAsync(IEnumerable<Guid> instrumentIds, DateOnly asOf, string currency, CancellationToken cancellationToken);
}
