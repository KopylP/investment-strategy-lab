namespace InvestmentLab.Rebalancing.Contracts;

public sealed record RebalancingTargetDto(Guid InstrumentId, decimal Weight);
