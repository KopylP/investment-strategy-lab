using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record HoldingLine(Guid InstrumentId, IReadOnlyList<VirtualLot> Lots)
{
    public Quantity Quantity => Quantity.Of(Lots.Sum(lot => lot.Quantity.Value));

    public decimal CostBasis => decimal.Round(Lots.Sum(lot => lot.Cost), 2);

    public decimal AverageCost => Quantity.IsNone ? 0m : Lots.Sum(lot => lot.Cost) / Quantity.Value;

    public DateOnly FirstOpenedOn => Lots.Min(lot => lot.OpenedOn);
}
