namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record PricesInCurrency(IReadOnlyDictionary<Guid, IInstrumentPrice> Prices, IReadOnlyList<string> Warnings);
