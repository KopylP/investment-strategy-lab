namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BookPosition(Guid InstrumentId, IReadOnlyList<BookLot> Lots)
{
    public decimal Quantity => Lots.Sum(lot => lot.Quantity);

    public decimal AverageCost => Lots.Sum(lot => lot.Quantity * lot.UnitCost) / Quantity;
}
