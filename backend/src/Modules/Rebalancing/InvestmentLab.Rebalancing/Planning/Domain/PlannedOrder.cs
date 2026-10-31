namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record PlannedOrder(OrderKind Kind, Guid InstrumentId, decimal Quantity, decimal Price, decimal Amount, string Reason)
{
    public OrderSide Side => Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Unpark ? OrderSide.Sell : OrderSide.Buy;
}
