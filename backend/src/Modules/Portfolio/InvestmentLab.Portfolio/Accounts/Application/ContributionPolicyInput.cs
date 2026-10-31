namespace InvestmentLab.Portfolio.Accounts.Application;

public sealed record ContributionPolicyInput(string Kind, decimal? DriftBandPercent);
