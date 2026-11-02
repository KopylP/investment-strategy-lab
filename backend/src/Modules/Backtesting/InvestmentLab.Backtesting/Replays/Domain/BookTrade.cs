namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BookTrade(Guid InstrumentId, DateOnly On, bool IsSale, decimal Quantity, decimal Price, decimal Fee)
{
    public static IReadOnlyList<BookTrade> HeldThroughout(Guid instrumentId, IReadOnlyList<GrowthDay> growth, decimal capital)
    {
        var first = growth[0];
        var last = growth[^1];
        var quantity = capital / (decimal)first.Growth;

        return
        [
            new BookTrade(instrumentId, first.Date, IsSale: false, quantity, (decimal)first.Growth, Fee: 0m),
            new BookTrade(instrumentId, last.Date, IsSale: true, quantity, (decimal)last.Growth, Fee: 0m),
        ];
    }
}
