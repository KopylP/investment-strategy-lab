namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IHeldLot
{
    decimal Quantity { get; }
    decimal UnitCost { get; }
    DateOnly OpenedOn { get; }
}
