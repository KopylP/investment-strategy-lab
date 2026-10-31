namespace InvestmentLab.Portfolio.Contracts.Holdings;

public sealed record SubPortfolioHoldingsDto(
    Guid SubPortfolioId,
    string Name,
    decimal Cash,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<CashBalanceDto>? ForeignCash = null);
