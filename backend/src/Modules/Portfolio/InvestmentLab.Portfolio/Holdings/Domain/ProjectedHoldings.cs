namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record ProjectedHoldings(AccountHoldings Holdings, IReadOnlyList<string> Warnings);
