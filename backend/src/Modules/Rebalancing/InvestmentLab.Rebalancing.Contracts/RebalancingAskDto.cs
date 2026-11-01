namespace InvestmentLab.Rebalancing.Contracts;

public sealed record RebalancingAskDto(
    bool MarketIsFavorable,
    decimal InvestFraction,
    IReadOnlyList<RebalancingTargetDto> Targets,
    IReadOnlyList<RebalancingPositionDto> Positions,
    decimal Cash,
    IReadOnlyDictionary<Guid, decimal> Prices,
    Guid? CashInstrumentId,
    string Currency);
