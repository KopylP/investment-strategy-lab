namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioPlanInput(
    StrategyTargets Strategy,
    IReadOnlyList<SubPortfolioPosition> Positions,
    decimal Cash,
    decimal Contribution,
    IReadOnlyDictionary<Guid, decimal> Prices,
    Guid? CashInstrumentId,
    RebalancingPolicy Policy,
    Currency Currency,
    IReadOnlyDictionary<Guid, string> Labels);
