namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioPlan(
    bool MarketIsFavorable,
    Percentage InvestFraction,
    decimal Contribution,
    Guid? CashInstrumentId,
    IReadOnlyList<PlannedOrder> Orders,
    IReadOnlyList<AllocationRow> Allocation,
    decimal TotalBefore,
    decimal TotalAfter,
    decimal ParkedBefore,
    decimal ParkedAfter,
    IReadOnlyList<string> Notes)
{
    public IEnumerable<PlannedOrder> Sells => Orders.Where(order => order.Side == OrderSide.Sell);
    public IEnumerable<PlannedOrder> Buys => Orders.Where(order => order.Side == OrderSide.Buy);
}
