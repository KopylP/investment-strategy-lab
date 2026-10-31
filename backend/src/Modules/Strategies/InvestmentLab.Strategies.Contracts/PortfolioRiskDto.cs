namespace InvestmentLab.Strategies.Contracts;

public sealed record PortfolioRiskDto(decimal Value, decimal Cost, decimal? PnlPct, decimal? Drawdown, DateOnly? PeakDate, bool ExitLimitHit, IReadOnlyList<string> Alerts);
