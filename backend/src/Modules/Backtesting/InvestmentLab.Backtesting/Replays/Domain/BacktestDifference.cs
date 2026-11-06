namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BacktestDifference(
    double AnnualReturn,
    double? AnnualReturnAfterTax,
    double Volatility,
    double Sharpe,
    double MaxDrawdown);
