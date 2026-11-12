namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record TickerMappingChoice(string Ticker, Guid? InstrumentId, string? RegisterAs, Guid? SubPortfolioId);
