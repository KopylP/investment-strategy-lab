namespace InvestmentLab.MarketData.Instruments.Application.Shared;

public sealed record SymbolQuery(IReadOnlyList<string> Candidates, string? Currency);
