namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record HoldingsDiagnostics(double? UnfavorableShare, double MeanInvestedShare, double MeanFunds, double TradesPerYear);
