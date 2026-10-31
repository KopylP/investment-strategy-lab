using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record TickerMappingInput(string Ticker, Guid? InstrumentId, string? RegisterAs, Guid? SubPortfolioId);
