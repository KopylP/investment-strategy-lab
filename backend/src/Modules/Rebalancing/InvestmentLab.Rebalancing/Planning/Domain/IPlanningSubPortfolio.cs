namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IPlanningSubPortfolio
{
    Guid Id { get; }
    string Name { get; }
    decimal TargetShare { get; }
    bool IsActive { get; }
    Guid? StrategyId { get; }
    int? PinnedStrategyVersion { get; }
    Guid? CashInstrumentId { get; }
    decimal? MaxPositionWeight { get; }
    decimal? MaxMonthlyTurnover { get; }
}
