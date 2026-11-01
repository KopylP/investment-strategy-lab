namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IHeldPosition
{
    Guid InstrumentId { get; }
    decimal Quantity { get; }
    decimal AverageCost { get; }
    IReadOnlyList<IHeldLot> Lots { get; }
}
