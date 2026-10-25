namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>Why an order exists. The order of the values is the order a broker should execute them in.</summary>
public enum OrderKind
{
    /// <summary>Sell a position the strategy no longer selects.</summary>
    Exit,

    /// <summary>Sell the excess of a winner far above its target.</summary>
    Trim,

    /// <summary>Sell the cash ETF to pay for buys.</summary>
    Unpark,

    /// <summary>Buy a pick up to its target.</summary>
    Buy,

    /// <summary>Buy the cash ETF with money that is not invested this month.</summary>
    Park,
}

public enum OrderSide
{
    Buy,
    Sell,
}

/// <summary>One order of a sub-portfolio, before netting with other sub-portfolios.</summary>
/// <param name="Amount">Value at the planned price; unrounded, so sums match the plan to the cent.</param>
public sealed record PlannedOrder(OrderKind Kind, Guid InstrumentId, decimal Quantity, decimal Price, decimal Amount, string Reason)
{
    public OrderSide Side => Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Unpark ? OrderSide.Sell : OrderSide.Buy;
}

public enum AllocationRole
{
    Pick,
    Exit,
    Cash,
}

/// <summary>A line of the "before → after" table of a sub-portfolio. The cash line has no instrument when nothing is parked in an ETF.</summary>
public sealed record AllocationRow(
    Guid? InstrumentId,
    AllocationRole Role,
    decimal ValueBefore,
    decimal ValueAfter,
    decimal WeightBefore,
    decimal WeightAfter,
    decimal? TargetWeight);

/// <summary>Orders of one sub-portfolio for the month and what the sub-portfolio looks like before and after them.</summary>
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
