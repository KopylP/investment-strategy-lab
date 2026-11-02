namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySummary(
    double TotalReturn,
    double AnnualReturn,
    double Volatility,
    double Sharpe,
    double MaxDrawdown,
    double? AnnualReturnAfterTax = null);
