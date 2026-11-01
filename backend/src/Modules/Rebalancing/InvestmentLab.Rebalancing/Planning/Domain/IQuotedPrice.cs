namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IQuotedPrice
{
    Guid InstrumentId { get; }
    decimal Close { get; }
    DateOnly Date { get; }
    string Currency { get; }
    bool IsStale { get; }
}
