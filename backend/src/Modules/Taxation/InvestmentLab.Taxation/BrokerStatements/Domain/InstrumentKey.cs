namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public sealed record InstrumentKey(string? Isin, IReadOnlyList<string> Symbols);
