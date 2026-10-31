namespace InvestmentLab.Performance.Valuation.Application;

public sealed record SubPortfolioPerformanceDto(Guid Id, string Name, decimal Value, decimal TargetShare, decimal ActualShare, decimal? Twr, decimal NetContributions);
