namespace InvestmentLab.Portfolio.Contracts.Holdings;

public sealed record AccountHoldingsDto(Guid AccountId, string Currency, DateOnly? AsOf, IReadOnlyList<SubPortfolioHoldingsDto> SubPortfolios);
