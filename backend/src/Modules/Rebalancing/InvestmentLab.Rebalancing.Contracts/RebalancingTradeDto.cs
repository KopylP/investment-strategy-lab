namespace InvestmentLab.Rebalancing.Contracts;

public sealed record RebalancingTradeDto(Guid InstrumentId, bool IsSale, decimal Quantity, decimal Amount);
