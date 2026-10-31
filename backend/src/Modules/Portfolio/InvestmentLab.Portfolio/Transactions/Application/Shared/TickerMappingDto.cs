using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record TickerMappingDto(
    string Ticker,
    string Currency,
    Guid? InstrumentId,
    string? RegisterAs,
    Guid? SubPortfolioId,
    int Rows,
    string? InstrumentName);
