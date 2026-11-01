namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IHeldSubPortfolio
{
    Guid Id { get; }
    decimal Cash { get; }
    IReadOnlyList<IHeldPosition> Positions { get; }
}
