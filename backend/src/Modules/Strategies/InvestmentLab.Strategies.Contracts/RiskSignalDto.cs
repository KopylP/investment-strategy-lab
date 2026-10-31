namespace InvestmentLab.Strategies.Contracts;

public sealed record RiskSignalDto(Guid InstrumentId, string Kind, string Reason, decimal? ExitPrice);
