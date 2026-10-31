namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed record GenerateRiskReviewCommand(Guid AccountId, DateOnly? AsOf = null, ReportScope? Scope = null);
