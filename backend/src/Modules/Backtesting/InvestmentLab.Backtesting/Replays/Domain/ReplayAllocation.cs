namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayAllocation(
    decimal Cash,
    IReadOnlyList<ReplayTarget> Targets,
    IReadOnlyList<ReplayPositionWeight> Positions)
{
    public static ReplayAllocation Of(ReplayBook book, IReadOnlyDictionary<Guid, decimal> prices, IReadOnlyList<ReplayTarget> targets)
    {
        var value = book.ValueAt(prices);
        var positions = book.Positions.Select(position => WeightOf(position, prices, value)).ToList();

        return new ReplayAllocation(book.Cash, targets, positions);
    }

    private static ReplayPositionWeight WeightOf(BookPosition position, IReadOnlyDictionary<Guid, decimal> prices, decimal portfolioValue)
    {
        var value = position.Quantity * prices.GetValueOrDefault(position.InstrumentId);
        var weight = portfolioValue > 0m ? value / portfolioValue : 0m;

        return new ReplayPositionWeight(position.InstrumentId, position.Quantity, weight);
    }
}
