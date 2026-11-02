namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayBook(decimal cash)
{
    private const decimal CostShareOfAmount = 0.001m;
    private const decimal FeePerTrade = 1m;
    private const decimal SmallestKeptShare = 0.000000001m;

    private readonly Dictionary<Guid, List<BookLot>> _lots = [];
    private readonly List<BookTrade> _trades = [];

    public decimal Cash { get; private set; } = cash;

    public int Trades => _trades.Count;

    public IReadOnlyList<BookTrade> TradeLog => _trades;

    public IReadOnlyList<BookPosition> Positions => [.. _lots.Select(entry => new BookPosition(entry.Key, entry.Value))];

    public decimal ValueAt(IReadOnlyDictionary<Guid, decimal> prices) =>
        Cash + Positions.Sum(position => position.Quantity * prices.GetValueOrDefault(position.InstrumentId));

    public void Buy(Guid instrumentId, decimal amount, decimal price, DateOnly day)
    {
        var affordable = Math.Max(0m, (Cash - FeePerTrade) / (1m + CostShareOfAmount));
        var spent = Math.Min(amount, affordable);

        if (spent <= 0m || price <= 0m)
            return;

        var cost = CostOf(spent);
        var quantity = spent / price;
        Cash -= spent + cost;
        _trades.Add(new BookTrade(instrumentId, day, IsSale: false, quantity, price, cost));

        if (!_lots.TryGetValue(instrumentId, out var lots))
            _lots[instrumentId] = lots = [];

        lots.Add(new BookLot(quantity, price, day));
    }

    public void Sell(Guid instrumentId, decimal quantity, decimal price, DateOnly day)
    {
        var held = _lots.TryGetValue(instrumentId, out var lots) ? lots.Sum(lot => lot.Quantity) : 0m;
        var sold = Math.Min(quantity, held);

        if (sold <= 0m || price <= 0m)
            return;

        Reduce(instrumentId, 1m - sold / held);

        var amount = sold * price;
        var cost = CostOf(amount);
        Cash += amount - cost;
        _trades.Add(new BookTrade(instrumentId, day, IsSale: true, sold, price, cost));
    }

    public IReadOnlyList<BookTrade> LiquidationOn(DateOnly day, IReadOnlyDictionary<Guid, decimal> prices)
    {
        var priced = Positions.Where(position => prices.GetValueOrDefault(position.InstrumentId) > 0m);

        return [.. priced.Select(position => SaleOf(position, day, prices[position.InstrumentId]))];
    }

    private static BookTrade SaleOf(BookPosition position, DateOnly day, decimal price) =>
        new(position.InstrumentId, day, IsSale: true, position.Quantity, price, CostOf(position.Quantity * price));

    private static decimal CostOf(decimal amount) => amount * CostShareOfAmount + FeePerTrade;

    private void Reduce(Guid instrumentId, decimal shareKept)
    {
        if (shareKept <= SmallestKeptShare)
        {
            _lots.Remove(instrumentId);
            return;
        }

        _lots[instrumentId] = [.. _lots[instrumentId].Select(lot => lot with { Quantity = lot.Quantity * shareKept })];
    }
}
