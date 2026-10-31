namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record BrokerOrder(OrderKind Kind, Guid InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<OrderAllocation> Allocations)
{
    public OrderSide Side => Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Unpark ? OrderSide.Sell : OrderSide.Buy;

    public decimal Amount => Quantity * Price;

    public string Reason => string.Join("; ", Allocations.Select(allocation => allocation.Reason).Distinct());
}
