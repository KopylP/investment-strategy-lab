namespace InvestmentLab.MarketData.Instruments.Application;

public sealed record SymbolQuery(IReadOnlyList<string> Candidates, string? Currency);
