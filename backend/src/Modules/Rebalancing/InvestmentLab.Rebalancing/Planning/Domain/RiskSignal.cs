namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record RiskSignal(Guid InstrumentId, string Kind, string Reason);
