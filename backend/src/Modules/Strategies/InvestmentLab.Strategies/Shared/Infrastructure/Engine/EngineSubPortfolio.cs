namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineSubPortfolio(string Currency, decimal Cash, decimal Contribution, IReadOnlyList<EngineHolding> Holdings);
