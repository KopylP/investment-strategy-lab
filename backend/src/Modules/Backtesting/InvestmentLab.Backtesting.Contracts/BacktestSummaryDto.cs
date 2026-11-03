namespace InvestmentLab.Backtesting.Contracts;

public sealed record BacktestSummaryDto(double TotalReturn, double AnnualReturn, double? AnnualReturnAfterTax, double Volatility, double Sharpe, double MaxDrawdown);
