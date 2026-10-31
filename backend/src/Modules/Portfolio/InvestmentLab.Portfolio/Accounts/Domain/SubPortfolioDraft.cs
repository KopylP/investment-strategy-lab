namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record SubPortfolioDraft(string Name, TargetShare TargetShare, StrategyRef? Strategy, Guid? CashInstrumentId, Guardrails Guardrails);
