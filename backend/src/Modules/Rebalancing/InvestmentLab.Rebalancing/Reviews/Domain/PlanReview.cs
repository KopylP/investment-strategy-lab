namespace InvestmentLab.Rebalancing.Reviews.Domain;

public sealed record PlanReview(string Reviewer, ReviewVerdict Verdict, string Summary, IReadOnlyList<ReviewFinding> Findings);
