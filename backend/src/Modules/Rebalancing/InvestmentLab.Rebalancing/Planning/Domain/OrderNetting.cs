namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>An order of one sub-portfolio, as input for netting.</summary>
public sealed record SubPortfolioOrder(Guid SubPortfolioId, PlannedOrder Order);

/// <summary>The part of a broker order that belongs to one sub-portfolio.</summary>
public sealed record OrderAllocation(Guid SubPortfolioId, OrderKind Kind, decimal Quantity, string Reason);

/// <summary>One order to place at the broker, after netting the sub-portfolios' orders of the same instrument.</summary>
public sealed record BrokerOrder(OrderKind Kind, Guid InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<OrderAllocation> Allocations)
{
    public OrderSide Side => Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Unpark ? OrderSide.Sell : OrderSide.Buy;

    public decimal Amount => Quantity * Price;

    public string Reason => string.Join("; ", Allocations.Select(allocation => allocation.Reason).Distinct());
}

/// <summary>Units one sub-portfolio sells and another buys: moved inside the account, no broker trade, no fee, no tax.</summary>
public sealed record InternalTransfer(Guid InstrumentId, Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Quantity, decimal Price, string Reason)
{
    public decimal Amount => Quantity * Price;
}

public sealed record NettedOrders(IReadOnlyList<BrokerOrder> BrokerOrders, IReadOnlyList<InternalTransfer> Transfers);

/// <summary>
/// Folds the orders of all sub-portfolios into broker orders. When one sub-portfolio sells what another buys, the
/// overlapping units become internal transfers and only the net goes to the broker.
/// </summary>
public static class OrderNetting
{
    public static NettedOrders Net(IReadOnlyList<SubPortfolioOrder> orders)
    {
        var brokerOrders = new List<BrokerOrder>();
        var transfers = new List<InternalTransfer>();

        foreach (var instrument in orders.GroupBy(entry => entry.Order.InstrumentId))
        {
            var price = instrument.First().Order.Price;
            var sells = Remaining(instrument.Where(entry => entry.Order.Side == OrderSide.Sell));
            var buys = Remaining(instrument.Where(entry => entry.Order.Side == OrderSide.Buy));

            int s = 0, b = 0;
            while (s < sells.Count && b < buys.Count)
            {
                var moved = Math.Min(sells[s].Quantity, buys[b].Quantity);
                transfers.Add(new InternalTransfer(instrument.Key, sells[s].SubPortfolioId, buys[b].SubPortfolioId, moved, price,
                    $"переказ між під-портфелями замість продажу й купівлі: {buys[b].Reason}"));
                sells[s] = sells[s] with { Quantity = sells[s].Quantity - moved };
                buys[b] = buys[b] with { Quantity = buys[b].Quantity - moved };
                if (sells[s].Quantity == 0m) s++;
                if (buys[b].Quantity == 0m) b++;
            }

            foreach (var side in new[] { sells, buys })
            {
                var left = side.Where(allocation => allocation.Quantity > 0m).ToList();
                if (left.Count > 0)
                    brokerOrders.Add(new BrokerOrder(left.Min(allocation => allocation.Kind), instrument.Key, left.Sum(allocation => allocation.Quantity), price, left));
            }
        }

        return new NettedOrders([.. brokerOrders.OrderBy(order => order.Kind)], transfers);
    }

    private static List<OrderAllocation> Remaining(IEnumerable<SubPortfolioOrder> orders) =>
        [.. orders.Select(entry => new OrderAllocation(entry.SubPortfolioId, entry.Order.Kind, entry.Order.Quantity, entry.Order.Reason))];
}
