namespace InvestmentLab.Rebalancing.Contracts;

public sealed record RebalancingPositionDto(Guid InstrumentId, decimal Quantity, decimal AverageCost);
