namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record InternalTransfer(Guid InstrumentId, Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Quantity, decimal Price, string Reason)
{
    public decimal Amount => Quantity * Price;
}
