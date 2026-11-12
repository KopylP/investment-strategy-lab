namespace InvestmentLab.MarketData.Contracts.Quotes;

public sealed record QuotePointDto(DateOnly Date, decimal Close, decimal AdjustedClose, IReadOnlyList<string> Flags);
