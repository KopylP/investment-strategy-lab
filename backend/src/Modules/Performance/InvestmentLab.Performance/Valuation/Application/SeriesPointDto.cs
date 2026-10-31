using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application;

public sealed record SeriesPointDto(DateOnly Date, decimal Value, decimal NetContributions, decimal TwrIndex, decimal? PlanValue, decimal? PlanNetContributions, decimal Drawdown, decimal PositionsValue, decimal CostBasis);
