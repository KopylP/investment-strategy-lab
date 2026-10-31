namespace InvestmentLab.Portfolio.Accounts.Application.Shared;

public sealed record ContributionPolicyInput(string Kind, decimal? DriftBandPercent);
