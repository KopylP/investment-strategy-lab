namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyJobTicket(string JobId, StrategyTask Task, Guid StrategyId, int StrategyVersion, Guid? CashInstrumentId);
