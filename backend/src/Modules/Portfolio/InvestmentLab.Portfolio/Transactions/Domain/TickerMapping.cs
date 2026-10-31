using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TickerMapping(
    string Ticker,
    string Currency,
    Guid? InstrumentId,
    string? RegisterAs,
    Guid? SubPortfolioId,
    string? RegisterName = null);
