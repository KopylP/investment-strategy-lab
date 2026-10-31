namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record ReportScope(string Code, string Title, IReadOnlySet<Guid> SubPortfolioIds);
