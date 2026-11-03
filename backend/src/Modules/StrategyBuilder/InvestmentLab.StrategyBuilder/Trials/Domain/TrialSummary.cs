namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public sealed record TrialSummary(TrialStatus Status, string? FailureReason, double? AnnualReturn, double? AnnualReturnAfterTax, double? Volatility, double? Sharpe, double? MaxDrawdown);
