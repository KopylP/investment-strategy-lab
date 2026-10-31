namespace InvestmentLab.Rebalancing.Planning.Domain;

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
                    brokerOrders.Add(new BrokerOrder(
                        left.Min(allocation => allocation.Kind),
                        instrument.Key,
                        left.Sum(allocation => allocation.Quantity),
                        price,
                        left));
            }
        }

        return new NettedOrders([.. brokerOrders.OrderBy(order => order.Kind)], transfers);
    }

    private static List<OrderAllocation> Remaining(IEnumerable<SubPortfolioOrder> orders) =>
        [.. orders.Select(entry => new OrderAllocation(entry.SubPortfolioId, entry.Order.Kind, entry.Order.Quantity, entry.Order.Reason))];
}
