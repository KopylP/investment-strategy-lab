namespace InvestmentLab.MarketData.Instruments.Application.Shared;

public sealed record SymbolMatch(string Symbol, string Name, string Currency, string QuoteType, string? Description = null);
