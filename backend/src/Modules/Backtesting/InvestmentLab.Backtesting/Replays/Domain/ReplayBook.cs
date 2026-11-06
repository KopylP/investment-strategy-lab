namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayBook(decimal cash, TradingCosts? costs = null)
{
    private const decimal SmallestKeptShare = 0.000000001m;

    private readonly TradingCosts _costs = costs ?? new TradingCosts();
    private readonly Dictionary<Guid, List<BookLot>> _lots = [];
    private readonly List<BookTrade> _trades = [];

    public decimal Cash { get; private set; } = cash;

    public int Trades => _trades.Count;

    public IReadOnlyList<BookTrade> TradeLog => _trades;

    public IReadOnlyList<BookPosition> Positions => [.. _lots.Select(entry => new BookPosition(entry.Key, entry.Value))];

    public BookTrade? Execute(ReplayTrade trade, decimal quote, DateOnly day)
    {
        var before = Trades;

        if (trade.IsSale)
            Sell(trade.InstrumentId, trade.Quantity, quote, day);
        else
            Buy(trade.InstrumentId, trade.Amount, quote, day);

        return Trades > before ? _trades[^1] : null;
    }

    public decimal ValueAt(IReadOnlyDictionary<Guid, decimal> prices) =>
        Cash + Positions.Sum(position => position.Quantity * prices.GetValueOrDefault(position.InstrumentId));

    public void Buy(Guid instrumentId, decimal amount, decimal quote, DateOnly day)
    {
        var affordable = Math.Max(0m, (Cash - _costs.FeePerTrade) / (1m + _costs.CommissionRate));
        var spent = Math.Min(amount, affordable);

        if (spent <= 0m || quote <= 0m)
            return;

        var fee = CostOf(spent);
        var fillPrice = _costs.FillPrice(quote, isSale: false);
        var quantity = spent / fillPrice;
        Cash -= spent + fee;
        _trades.Add(TradeOf(instrumentId, day, isSale: false, quantity, quote, fee));

        if (!_lots.TryGetValue(instrumentId, out var lots))
            _lots[instrumentId] = lots = [];

        lots.Add(new BookLot(quantity, fillPrice, day));
    }

    public void Sell(Guid instrumentId, decimal quantity, decimal quote, DateOnly day)
    {
        var held = _lots.TryGetValue(instrumentId, out var lots) ? lots.Sum(lot => lot.Quantity) : 0m;
        var sold = Math.Min(quantity, held);

        if (sold <= 0m || quote <= 0m)
            return;

        Reduce(instrumentId, 1m - sold / held);

        var fillPrice = _costs.FillPrice(quote, isSale: true);
        var amount = sold * fillPrice;
        var fee = CostOf(amount);
        Cash += amount - fee;
        _trades.Add(TradeOf(instrumentId, day, isSale: true, sold, quote, fee));
    }

    public IReadOnlyList<BookTrade> LiquidationOn(DateOnly day, IReadOnlyDictionary<Guid, decimal> prices)
    {
        var priced = Positions.Where(position => prices.GetValueOrDefault(position.InstrumentId) > 0m);

        return [.. priced.Select(position => SaleOf(position, day, prices[position.InstrumentId]))];
    }

    private BookTrade SaleOf(BookPosition position, DateOnly day, decimal quote)
    {
        var fillPrice = _costs.FillPrice(quote, isSale: true);
        var fee = CostOf(position.Quantity * fillPrice);

        return TradeOf(position.InstrumentId, day, isSale: true, position.Quantity, quote, fee);
    }

    private decimal CostOf(decimal amount) => _costs.FeeOf(amount);

    private BookTrade TradeOf(Guid instrumentId, DateOnly day, bool isSale, decimal quantity, decimal quote, decimal fee) =>
        new(
            InstrumentId: instrumentId,
            On: day,
            IsSale: isSale,
            Quantity: quantity,
            Price: _costs.FillPrice(quote, isSale),
            Fee: fee,
            ReferencePrice: quote,
            SpreadCost: _costs.SpreadCostOf(quantity, quote),
            SlippageCost: _costs.SlippageCostOf(quantity, quote));

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
