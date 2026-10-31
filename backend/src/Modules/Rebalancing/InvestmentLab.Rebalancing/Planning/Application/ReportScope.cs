namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record ReportScope(string Code, string Title, IReadOnlySet<Guid> SubPortfolioIds);
