namespace InvestmentLab.Performance.Valuation.Domain;

public sealed record Snapshot(
    DateOnly Date,
    Guid? SubPortfolioId,
    decimal Value,
    decimal Cash,
    decimal Flow,
    decimal NetContributions,
    decimal TwrIndex,
    decimal CostBasis = 0m);
