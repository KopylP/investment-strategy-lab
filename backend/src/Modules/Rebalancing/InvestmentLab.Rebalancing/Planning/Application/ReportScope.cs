namespace InvestmentLab.Rebalancing.Planning.Application;

/// <summary>Which of the strategies' reports to make, and for which of the account's sub-portfolios (those whose strategy makes it).</summary>
public sealed record ReportScope(string Code, string Title, IReadOnlySet<Guid> SubPortfolioIds);
