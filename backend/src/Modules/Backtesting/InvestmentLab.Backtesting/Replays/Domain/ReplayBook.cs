namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayBook(decimal cash)
{
    private const decimal CostShareOfAmount = 0.001m;
    private const decimal FeePerTrade = 1m;
    private const decimal SmallestKeptShare = 0.000000001m;

    private readonly Dictionary<Guid, List<BookLot>> _lots = [];

    public decimal Cash { get; private set; } = cash;

    public int Trades { get; private set; }

    public IReadOnlyList<BookPosition> Positions => [.. _lots.Select(entry => new BookPosition(entry.Key, entry.Value))];

    public decimal ValueAt(IReadOnlyDictionary<Guid, decimal> prices) =>
        Cash + Positions.Sum(position => position.Quantity * prices.GetValueOrDefault(position.InstrumentId));

    public void Buy(Guid instrumentId, decimal amount, decimal price, DateOnly day)
    {
        var affordable = Math.Max(0m, (Cash - FeePerTrade) / (1m + CostShareOfAmount));
        var spent = Math.Min(amount, affordable);

        if (spent <= 0m || price <= 0m)
            return;

        Cash -= spent + PayCostOf(spent);

        if (!_lots.TryGetValue(instrumentId, out var lots))
            _lots[instrumentId] = lots = [];

        lots.Add(new BookLot(spent / price, price, day));
    }

    public void Sell(Guid instrumentId, decimal quantity, decimal price)
    {
        var held = _lots.TryGetValue(instrumentId, out var lots) ? lots.Sum(lot => lot.Quantity) : 0m;
        var sold = Math.Min(quantity, held);

        if (sold <= 0m || price <= 0m)
            return;

        Reduce(instrumentId, 1m - sold / held);

        var amount = sold * price;
        Cash += amount - PayCostOf(amount);
    }

    private decimal PayCostOf(decimal amount)
    {
        Trades++;

        return amount * CostShareOfAmount + FeePerTrade;
    }

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
