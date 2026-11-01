namespace InvestmentLab.Signals.Monitoring.Domain;

public interface IOpenPosition
{
    Guid InstrumentId { get; }
    decimal Quantity { get; }
    decimal CostBasis { get; }
    DateOnly FirstOpenedOn { get; }
}
