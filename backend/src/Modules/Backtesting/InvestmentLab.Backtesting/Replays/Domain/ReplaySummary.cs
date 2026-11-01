namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySummary(double AnnualReturn, double Volatility, double Sharpe, double MaxDrawdown);
