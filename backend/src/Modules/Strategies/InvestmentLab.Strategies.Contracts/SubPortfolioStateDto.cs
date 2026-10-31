namespace InvestmentLab.Strategies.Contracts;

public sealed record SubPortfolioStateDto(string Currency, decimal Cash, IReadOnlyList<HoldingStateDto> Holdings);
