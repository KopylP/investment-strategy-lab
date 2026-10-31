namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyJob(string JobId, string Task, Guid StrategyId, int StrategyVersion, Guid? CashInstrumentId);
