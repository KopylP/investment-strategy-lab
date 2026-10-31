namespace InvestmentLab.MarketData.Instruments.Application;

public sealed record SymbolMatch(string Symbol, string Name, string Currency, string QuoteType, string? Description = null);
