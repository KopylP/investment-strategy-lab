namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IInstrumentPrice
{
    decimal Close { get; }
    DateOnly Date { get; }
    bool IsStale { get; }

    decimal FromQuoteCurrency(decimal amount);
}
