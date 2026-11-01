namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record BrokerOrder(OrderKind Kind, Guid InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<OrderAllocation> Allocations)
{
    public OrderSide Side => Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Unpark ? OrderSide.Sell : OrderSide.Buy;

    public decimal Amount => Quantity * Price;

    public string Reason
    {
        get
        {
            var reasons = Allocations.Select(allocation => allocation.Reason).Distinct().ToList();

            return reasons.Count == 1 ? reasons[0] : Message.Of("report.reason.several", new { reasons });
        }
    }
}
